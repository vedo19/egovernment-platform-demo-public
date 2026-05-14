using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Services.Events;

public class NotificationConsumer : BackgroundService
{
    public const string ExchangeName = "egov.events";
    public const string QueueName = "notifications.events";
    public const string RoutingKey = "#";

    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotificationConsumer(IServiceProvider services, IConfiguration configuration, ILogger<NotificationConsumer> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => StartWithRetryAsync(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task StartWithRetryAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Connect();
                _logger.LogInformation("NotificationConsumer connected to RabbitMQ and consuming '{Queue}'", QueueName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotificationConsumer failed to connect to RabbitMQ; retrying in 5s");
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (TaskCanceledException) { return; }
            }
        }
    }

    private void Connect()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672,
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
        };

        _connection = factory.CreateConnection("notification-service-consumer");
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, RoutingKey);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 16, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;
        _channel.BasicConsume(QueueName, autoAck: false, consumer);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);

        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(json, JsonOptions);
            if (envelope is null)
            {
                _logger.LogWarning("Discarded null envelope on {RoutingKey}", routingKey);
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = _services.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<EventRouter>();
            await router.HandleAsync(routingKey, envelope, CancellationToken.None);

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing message on {RoutingKey}: {Body}", routingKey, json);
            // Ack to avoid poison-message loop. A production system would dead-letter.
            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        try { _channel?.Dispose(); } catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

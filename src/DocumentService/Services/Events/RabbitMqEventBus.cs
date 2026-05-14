using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace DocumentService.Services.Events;

public class RabbitMqEventBus : IEventBus, IDisposable
{
    public const string ExchangeName = "egov.events";

    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly object _connectLock = new();
    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqEventBus(IConfiguration configuration, ILogger<RabbitMqEventBus> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Publish(string routingKey, object payload)
    {
        try
        {
            EnsureChannel();
            if (_channel is null) return;

            var envelope = new
            {
                eventType = routingKey,
                occurredAt = DateTime.UtcNow,
                payload
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, JsonOptions));

            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {RoutingKey}; continuing fire-and-forget", routingKey);
        }
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;

        lock (_connectLock)
        {
            if (_channel is { IsOpen: true }) return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672,
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                    AutomaticRecoveryEnabled = true
                };

                _connection = factory.CreateConnection("document-service-publisher");
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection from DocumentService");
                _channel = null;
            }
        }
    }

    public void Dispose()
    {
        try { _channel?.Dispose(); } catch { /* ignore */ }
        try { _connection?.Dispose(); } catch { /* ignore */ }
        GC.SuppressFinalize(this);
    }
}

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentService.Services;

public sealed class ServiceRequestCreatedConsumer : BackgroundService
{
    private const string ExchangeName = "egov.events";
    private const string QueueName = "document-service.servicerequest.created";
    private const string RoutingKey = "servicerequest.created";

    private readonly ILogger<ServiceRequestCreatedConsumer> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public ServiceRequestCreatedConsumer(IConfiguration configuration, ILogger<ServiceRequestCreatedConsumer> logger)
    {
        _logger = logger;
        _host = configuration["RabbitMQ:Host"] ?? "localhost";
        _port = int.TryParse(configuration["RabbitMQ:Port"], out var configuredPort) ? configuredPort : 5672;
        _username = configuration["RabbitMQ:Username"] ?? "guest";
        _password = configuration["RabbitMQ:Password"] ?? "guest";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _host,
                    Port = _port,
                    UserName = _username,
                    Password = _password,
                    DispatchConsumersAsync = true
                };

                using var connection = factory.CreateConnection("document-service-consumer");
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (_, eventArgs) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                        var evt = JsonSerializer.Deserialize<ServiceRequestCreatedEvent>(json);

                        if (evt is null)
                        {
                            _logger.LogWarning("Received empty ServiceRequestCreated event payload.");
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Consumed RabbitMQ event {RoutingKey} for request {ServiceRequestId}, citizen {CitizenUserId}.",
                                RoutingKey,
                                evt.ServiceRequestId,
                                evt.CitizenUserId);
                        }

                        channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process RabbitMQ message; message will be rejected.");
                        channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }

                    await Task.CompletedTask;
                };

                channel.BasicConsume(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation(
                    "RabbitMQ consumer attached to queue {QueueName} on {Host}:{Port}.",
                    QueueName,
                    _host,
                    _port);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer connection failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}

public sealed record ServiceRequestCreatedEvent(
    Guid ServiceRequestId,
    Guid CitizenUserId,
    string Type,
    string Title,
    string Status,
    DateTime CreatedAtUtc);

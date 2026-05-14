using System.Text.Json;
using RabbitMQ.Client;
using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public sealed class RabbitMqServiceRequestEventPublisher : IServiceRequestEventPublisher
{
    private const string ExchangeName = "egov.events";
    private const string RoutingKey = "servicerequest.created";

    private readonly ILogger<RabbitMqServiceRequestEventPublisher> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public RabbitMqServiceRequestEventPublisher(IConfiguration configuration, ILogger<RabbitMqServiceRequestEventPublisher> logger)
    {
        _logger = logger;
        _host = configuration["RabbitMQ:Host"] ?? "localhost";
        _port = int.TryParse(configuration["RabbitMQ:Port"], out var configuredPort) ? configuredPort : 5672;
        _username = configuration["RabbitMQ:Username"] ?? "guest";
        _password = configuration["RabbitMQ:Password"] ?? "guest";
    }

    public Task PublishCreatedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory
        {
            HostName = _host,
            Port = _port,
            UserName = _username,
            Password = _password
        };

        var evt = new ServiceRequestCreatedEvent(
            serviceRequest.Id,
            serviceRequest.CitizenUserId,
            serviceRequest.Type,
            serviceRequest.Title,
            serviceRequest.Status,
            serviceRequest.CreatedAt);

        var payload = JsonSerializer.SerializeToUtf8Bytes(evt);

        try
        {
            using var connection = factory.CreateConnection("service-request-publisher");
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: payload);

            _logger.LogInformation(
                "Published RabbitMQ event {RoutingKey} for service request {ServiceRequestId}.",
                RoutingKey,
                serviceRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish RabbitMQ event {RoutingKey} for service request {ServiceRequestId}.",
                RoutingKey,
                serviceRequest.Id);
        }

        return Task.CompletedTask;
    }
}

public sealed record ServiceRequestCreatedEvent(
    Guid ServiceRequestId,
    Guid CitizenUserId,
    string Type,
    string Title,
    string Status,
    DateTime CreatedAtUtc);

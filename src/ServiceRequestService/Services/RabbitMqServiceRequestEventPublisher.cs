using System.Text.Json;
using RabbitMQ.Client;
using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public sealed class RabbitMqServiceRequestEventPublisher : IServiceRequestEventPublisher
{
    private const string ExchangeName = "egov.events";

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

    public Task PublishSubmittedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.submitted", serviceRequest, cancellationToken);

    public Task PublishAssignedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.assigned", serviceRequest, cancellationToken);

    public Task PublishDocumentsRequestedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.documents_requested", serviceRequest, cancellationToken);

    public Task PublishDocumentsRejectedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.documents_rejected", serviceRequest, cancellationToken);

    public Task PublishApprovedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.approved", serviceRequest, cancellationToken);

    public Task PublishRejectedAsync(ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        => PublishAsync("servicerequest.rejected", serviceRequest, cancellationToken);

    private Task PublishAsync(string routingKey, ServiceRequest serviceRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var factory = new ConnectionFactory
        {
            HostName = _host,
            Port = _port,
            UserName = _username,
            Password = _password
        };

        var envelope = new
        {
            eventType = routingKey,
            occurredAt = DateTime.UtcNow,
            payload = new
            {
                serviceRequestId = serviceRequest.Id,
                citizenUserId = serviceRequest.CitizenUserId,
                assignedOfficerId = serviceRequest.AssignedOfficerId,
                type = serviceRequest.Type,
                title = serviceRequest.Title,
                status = serviceRequest.Status,
                officerNote = serviceRequest.OfficerNote,
                reason = serviceRequest.AdminNotes,
                createdAtUtc = serviceRequest.CreatedAt
            }
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

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
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: payload);

            _logger.LogInformation(
                "Published RabbitMQ event {RoutingKey} for service request {ServiceRequestId}.",
                routingKey,
                serviceRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish RabbitMQ event {RoutingKey} for service request {ServiceRequestId}.",
                routingKey,
                serviceRequest.Id);
        }

        return Task.CompletedTask;
    }
}

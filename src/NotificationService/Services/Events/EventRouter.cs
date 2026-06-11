using System.Text.Json;

namespace NotificationService.Services.Events;

public class EventRouter
{
    private readonly INotificationService _notifications;
    private readonly IUserDirectory _users;
    private readonly ILogger<EventRouter> _logger;

    public EventRouter(INotificationService notifications, IUserDirectory users, ILogger<EventRouter> logger)
    {
        _notifications = notifications;
        _users = users;
        _logger = logger;
    }

    public async Task HandleAsync(string routingKey, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Routing event {RoutingKey} occurredAt={OccurredAt}", routingKey, envelope.OccurredAt);

        switch (routingKey)
        {
            case "document.submitted":
                await NotifyAdmins(
                    type: "DocumentSubmitted",
                    title: "New document request awaiting assignment",
                    message: $"Citizen submitted a new \"{P(envelope, "title")}\" document request. Please assign an officer.",
                    link: "/admin?tab=documents",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "document.assigned":
                await NotifyUser(
                    userId: G(envelope, "assignedOfficerId"),
                    type: "DocumentAssigned",
                    title: "A document was assigned to you",
                    message: $"You have been assigned the document request \"{P(envelope, "title")}\". Please begin review.",
                    link: "/officer?tab=documents",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "document.approved":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "DocumentReady",
                    title: "Your document is ready",
                    message: $"Your document \"{P(envelope, "title")}\" has been approved and is ready for download.",
                    link: "/citizen?tab=documents",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "document.rejected":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "DocumentRejected",
                    title: "Your document was rejected",
                    message: $"Your document \"{P(envelope, "title")}\" was rejected. Reason: {P(envelope, "reason")}",
                    link: "/citizen?tab=documents",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.submitted":
                await NotifyAdmins(
                    type: "ServiceRequestSubmitted",
                    title: "New service request awaiting assignment",
                    message: $"New {P(envelope, "type")} \"{P(envelope, "title")}\" was submitted. Please assign an officer.",
                    link: "/admin?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.assigned":
                await NotifyUser(
                    userId: G(envelope, "assignedOfficerId"),
                    type: "ServiceRequestAssigned",
                    title: "A service request was assigned to you",
                    message: $"You have been assigned the service request \"{P(envelope, "title")}\". Please begin review.",
                    link: "/officer?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.documents_requested":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "ServiceRequestDocumentsRequested",
                    title: "Action required: upload supporting documents",
                    message: $"The reviewing officer requested documents for \"{P(envelope, "title")}\". Note: {P(envelope, "officerNote")}",
                    link: "/citizen?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.documents_rejected":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "ServiceRequestDocumentsRejected",
                    title: "Submitted documents were rejected",
                    message: $"Your uploaded documents for \"{P(envelope, "title")}\" were rejected. Please resubmit. Reason: {P(envelope, "reason")}",
                    link: "/citizen?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.approved":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "ServiceRequestApproved",
                    title: "Your request was approved",
                    message: $"Your request \"{P(envelope, "title")}\" has been approved.",
                    link: "/citizen?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            case "servicerequest.rejected":
                await NotifyUser(
                    userId: G(envelope, "citizenUserId"),
                    type: "ServiceRequestRejected",
                    title: "Your request was rejected",
                    message: $"Your request \"{P(envelope, "title")}\" was rejected. Reason: {P(envelope, "reason")}",
                    link: "/citizen?tab=requests",
                    payload: envelope.Payload,
                    cancellationToken: cancellationToken);
                break;

            default:
                _logger.LogInformation("Ignoring unknown routing key {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task NotifyAdmins(
        string type,
        string title,
        string message,
        string link,
        IDictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        var admins = await _users.GetUsersByRoleAsync("Admin", cancellationToken);
        if (admins.Count == 0)
        {
            _logger.LogWarning("No admins found to notify for event {Type}", type);
            return;
        }

        var dataJson = JsonSerializer.Serialize(payload);
        foreach (var admin in admins)
        {
            await _notifications.DeliverAsync(
                userId: admin.Id,
                type: type,
                title: title,
                message: message,
                link: link,
                dataJson: dataJson,
                recipientForEmail: admin,
                cancellationToken: cancellationToken);
        }
    }

    private async Task NotifyUser(
        Guid userId,
        string type,
        string title,
        string message,
        string link,
        IDictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("Skipping {Type} notification: missing target user id", type);
            return;
        }

        var user = await _users.GetUserAsync(userId, cancellationToken);
        var dataJson = JsonSerializer.Serialize(payload);

        await _notifications.DeliverAsync(
            userId: userId,
            type: type,
            title: title,
            message: message,
            link: link,
            dataJson: dataJson,
            recipientForEmail: user,
            cancellationToken: cancellationToken);
    }

    private static string P(EventEnvelope envelope, string key)
    {
        if (!envelope.Payload.TryGetValue(key, out var value) || value is null)
            return string.Empty;

        if (value is JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? string.Empty,
                JsonValueKind.Number => el.ToString(),
                JsonValueKind.True or JsonValueKind.False => el.ToString(),
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => el.ToString()
            };
        }

        return value.ToString() ?? string.Empty;
    }

    private static Guid G(EventEnvelope envelope, string key)
    {
        var raw = P(envelope, key);
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }
}

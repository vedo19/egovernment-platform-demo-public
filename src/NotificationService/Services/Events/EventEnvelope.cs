namespace NotificationService.Services.Events;

public class EventEnvelope
{
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> Payload { get; set; } = new();
}

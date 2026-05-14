using NotificationService.DTOs;

namespace NotificationService.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> ListAsync(Guid userId, int take, int skip, CancellationToken cancellationToken = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DeliverAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? link,
        string? dataJson,
        UserInfo? recipientForEmail,
        CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Hubs;
using NotificationService.Models;

namespace NotificationService.Services;

public class NotificationServiceImpl : INotificationService
{
    private readonly NotificationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IEmailSender _email;
    private readonly ILogger<NotificationServiceImpl> _logger;
    private readonly string _platformBaseUrl;

    public NotificationServiceImpl(
        NotificationDbContext db,
        IHubContext<NotificationHub> hub,
        IEmailSender email,
        IConfiguration configuration,
        ILogger<NotificationServiceImpl> logger)
    {
        _db = db;
        _hub = hub;
        _email = email;
        _logger = logger;
        _platformBaseUrl = configuration["Platform:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
    }

    public async Task<IEnumerable<NotificationDto>> ListAsync(Guid userId, int take, int skip, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 100);
        var safeSkip = Math.Max(skip, 0);

        var rows = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(safeSkip)
            .Take(safeTake)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications.FindAsync(new object[] { notificationId }, cancellationToken);
        if (notification is null || notification.UserId != userId)
            return false;

        if (notification.IsRead)
            return true;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now),
                cancellationToken);
        return updated;
    }

    public async Task DeliverAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? link,
        string? dataJson,
        UserInfo? recipientForEmail,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            DataJson = dataJson,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            EmailSent = false
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(notification);

        try
        {
            await _hub.Clients
                .Group(NotificationHub.GroupName(userId))
                .SendAsync("ReceiveNotification", dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push real-time notification {Id} to user {UserId}", notification.Id, userId);
        }

        if (recipientForEmail is not null && !string.IsNullOrWhiteSpace(recipientForEmail.Email))
        {
            try
            {
                var subject = $"[E-Government] {title}";
                var body = BuildEmailBody(recipientForEmail.FullName, title, message, link);
                await _email.SendAsync(recipientForEmail.Email, recipientForEmail.FullName, subject, body);

                notification.EmailSent = true;
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification {Id} to {Email}", notification.Id, recipientForEmail.Email);
            }
        }
    }

    private string BuildEmailBody(string recipientName, string title, string message, string? link)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(recipientName);
        var safeTitle = System.Net.WebUtility.HtmlEncode(title);
        var safeMessage = System.Net.WebUtility.HtmlEncode(message);
        var ctaSection = string.Empty;
        if (!string.IsNullOrWhiteSpace(link))
        {
            var url = link.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? link : $"{_platformBaseUrl}{link}";
            var safeUrl = System.Net.WebUtility.HtmlEncode(url);
            ctaSection = $"<p style=\"margin-top:24px\"><a href=\"{safeUrl}\" style=\"background:#0057b8;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none;font-weight:600\">Open in platform</a></p>";
        }

        return $@"<!DOCTYPE html>
<html><body style=""font-family:Segoe UI,Arial,sans-serif;color:#1f2937;line-height:1.5"">
  <p>Hi {safeName},</p>
  <h2 style=""color:#0057b8;margin:0 0 12px 0"">{safeTitle}</h2>
  <p>{safeMessage}</p>
  {ctaSection}
  <hr style=""border:none;border-top:1px solid #e5e7eb;margin:32px 0"" />
  <p style=""color:#6b7280;font-size:12px"">This is an automated message from the E-Government Platform. Please do not reply.</p>
</body></html>";
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        Link = n.Link,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}

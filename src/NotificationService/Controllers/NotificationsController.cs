using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    /// <summary>List notifications for the authenticated user.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 20, [FromQuery] int skip = 0)
    {
        var userId = GetUserId();
        var list = await _notifications.ListAsync(userId, take, skip);
        return Ok(list);
    }

    /// <summary>Get the unread count for the authenticated user.</summary>
    [Authorize]
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = GetUserId();
        var count = await _notifications.UnreadCountAsync(userId);
        return Ok(new { count });
    }

    /// <summary>Mark a single notification as read.</summary>
    [Authorize]
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        var ok = await _notifications.MarkReadAsync(userId, id);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Mark all notifications as read.</summary>
    [Authorize]
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        var updated = await _notifications.MarkAllReadAsync(userId);
        return Ok(new { updated });
    }

    /// <summary>Health check.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "NotificationService" });

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Missing sub claim.");
        return Guid.Parse(sub);
    }
}

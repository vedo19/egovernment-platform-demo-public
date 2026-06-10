namespace NotificationService.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string bodyHtml, CancellationToken cancellationToken = default);
}

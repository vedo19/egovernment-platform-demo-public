using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotificationService.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string _host;
    private readonly int _port;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly bool _useStartTls;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
        _host = configuration["Smtp:Host"] ?? "localhost";
        _port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 1025;
        _username = configuration["Smtp:Username"];
        _password = configuration["Smtp:Password"];
        _fromAddress = configuration["Smtp:From"] ?? "noreply@egovernment.local";
        _fromName = configuration["Smtp:FromName"] ?? "E-Government Platform";
        _useStartTls = bool.TryParse(configuration["Smtp:UseStartTls"], out var s) && s;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

        using var smtp = new SmtpClient();
        var socketOption = _useStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;

        await smtp.ConnectAsync(_host, _port, socketOption, cancellationToken);
        if (!string.IsNullOrEmpty(_username))
        {
            await smtp.AuthenticateAsync(_username, _password, cancellationToken);
        }
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email sent to {Email} subject={Subject}", toEmail, subject);
    }
}

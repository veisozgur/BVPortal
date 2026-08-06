using System.Net;
using System.Net.Mail;
using BV.Application.Abstractions.Notifications;
using Microsoft.Extensions.Options;

namespace BV.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions settings = options.Value;

    public async Task SendAsync(
        string email,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = message,
            IsBodyHtml = false
        };
        mailMessage.To.Add(new MailAddress(email));

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(settings.Username, settings.Password)
        };

        await client.SendMailAsync(mailMessage, cancellationToken);
    }

    private void ValidateConfiguration()
    {
        if (!settings.Enabled)
            throw new InvalidOperationException("SMTP service is disabled.");
        if (string.IsNullOrWhiteSpace(settings.Host))
            throw new InvalidOperationException("SMTP host is not configured.");
        if (settings.Port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP port is invalid.");
        if (string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("SMTP sender address is not configured.");
    }
}

using BV.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace BV.Infrastructure.Notifications;

public sealed class DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string email, string subject, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Development email to {Email}. Subject: {Subject}. Message: {Message}", email, subject, message);
        return Task.CompletedTask;
    }
}

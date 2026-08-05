using BV.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace BV.Infrastructure.Notifications;

public sealed class DevelopmentSmsSender(ILogger<DevelopmentSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Development SMS sent to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}

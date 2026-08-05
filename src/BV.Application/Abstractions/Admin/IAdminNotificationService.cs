namespace BV.Application.Abstractions.Admin;

public interface IAdminNotificationService
{
    Task<AdminNotificationRetryResult> RetryAsync(Guid notificationId, CancellationToken cancellationToken = default);
}

public sealed record AdminNotificationRetryResult(
    Guid NotificationId,
    Guid QuoteRequestId,
    string Channel,
    string Destination,
    string Status,
    DateTime? SentAtUtc,
    string? ErrorMessage);

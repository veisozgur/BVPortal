namespace BV.Application.Abstractions.Notifications;

public interface IQuoteNotificationService
{
    Task NotifyAnsweredAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);
}

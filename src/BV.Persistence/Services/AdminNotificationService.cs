using BV.Application.Abstractions.Admin;
using BV.Application.Abstractions.Notifications;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Services;

public sealed class AdminNotificationService(
    BVPortalDbContext dbContext,
    ISmsSender smsSender,
    IEmailSender emailSender) : IAdminNotificationService
{
    public async Task<AdminNotificationRetryResult> RetryAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.QuoteNotifications
            .SingleOrDefaultAsync(x => x.Id == notificationId, cancellationToken)
            ?? throw new InvalidOperationException("Bildirim kaydı bulunamadı.");

        if (notification.Status == "Sent")
            throw new InvalidOperationException("Başarılı bildirim yeniden gönderilemez.");

        try
        {
            var message = $"BV Portal teklifiniz cevaplandı. Teklif No: {notification.QuoteRequestId}";

            if (notification.Channel.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                await smsSender.SendAsync(notification.Destination, message, cancellationToken);
            }
            else if (notification.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                await emailSender.SendAsync(
                    notification.Destination,
                    "Teklifiniz cevaplandı",
                    message,
                    cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Desteklenmeyen bildirim kanalı.");
            }

            notification.MarkSent();
        }
        catch (Exception exception)
        {
            notification.MarkFailed(exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdminNotificationRetryResult(
            notification.Id,
            notification.QuoteRequestId,
            notification.Channel,
            notification.Destination,
            notification.Status,
            notification.SentAtUtc,
            notification.ErrorMessage);
    }
}

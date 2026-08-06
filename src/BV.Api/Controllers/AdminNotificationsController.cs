using BV.Application.Abstractions.Admin;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/notifications")]
public sealed class AdminNotificationsController(
    IAdminNotificationService notificationService,
    BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? channel = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from notification in dbContext.QuoteNotifications.AsNoTracking()
            join quote in dbContext.QuoteRequests.AsNoTracking()
                on notification.QuoteRequestId equals quote.Id
            select new
            {
                notification.Id,
                notification.QuoteRequestId,
                quote.Title,
                notification.Channel,
                notification.Destination,
                notification.Status,
                notification.ErrorMessage,
                notification.CreatedAtUtc,
                notification.SentAtUtc
            };

        if (!string.IsNullOrWhiteSpace(channel))
            query = query.Where(x => x.Channel == channel.Trim());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status.Trim());

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var summary = await dbContext.QuoteNotifications
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            sentCount = summary.Where(x => x.Status == "Sent").Sum(x => x.Count),
            failedCount = summary.Where(x => x.Status == "Failed").Sum(x => x.Count),
            pendingCount = summary.Where(x => x.Status == "Pending").Sum(x => x.Count)
        });
    }

    [HttpPost("{notificationId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await notificationService.RetryAsync(notificationId, cancellationToken);
        return Ok(result);
    }
}

using BV.Domain.Quotes;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reports")]
public sealed class AdminReportsController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 365);
        var fromUtc = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var quoteQuery = dbContext.QuoteRequests
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= fromUtc);

        var total = await quoteQuery.CountAsync(cancellationToken);
        var submitted = await quoteQuery.CountAsync(x => x.Status != QuoteRequestStatus.Draft, cancellationToken);
        var answered = await quoteQuery.CountAsync(x => x.AnsweredAtUtc != null, cancellationToken);
        var accepted = await quoteQuery.CountAsync(x => x.Status == QuoteRequestStatus.Accepted, cancellationToken);
        var rejected = await quoteQuery.CountAsync(x => x.Status == QuoteRequestStatus.Rejected, cancellationToken);
        var school = await quoteQuery.CountAsync(x => x.Type == QuoteRequestType.School, cancellationToken);
        var office = await quoteQuery.CountAsync(x => x.Type == QuoteRequestType.Office, cancellationToken);

        var responseMinutes = await quoteQuery
            .Where(x => x.SubmittedAtUtc != null && x.AnsweredAtUtc != null)
            .Select(x => EF.Functions.DateDiffMinute(x.SubmittedAtUtc!.Value, x.AnsweredAtUtc!.Value))
            .ToListAsync(cancellationToken);

        var daily = await quoteQuery
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(group => new
            {
                date = group.Key,
                created = group.Count(),
                answered = group.Count(x => x.AnsweredAtUtc != null),
                accepted = group.Count(x => x.Status == QuoteRequestStatus.Accepted)
            })
            .OrderBy(x => x.date)
            .ToListAsync(cancellationToken);

        var topProducts = await dbContext.QuoteRequestItems
            .AsNoTracking()
            .Where(x => dbContext.QuoteRequests
                .Any(q => q.Id == EF.Property<Guid>(x, "QuoteRequestId") && q.CreatedAtUtc >= fromUtc))
            .GroupBy(x => x.ProductName)
            .Select(group => new
            {
                productName = group.Key,
                requestCount = group.Count(),
                totalQuantity = group.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.requestCount)
            .ThenByDescending(x => x.totalQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            periodDays = days,
            total,
            submitted,
            answered,
            accepted,
            rejected,
            school,
            office,
            answerRate = submitted == 0 ? 0 : Math.Round(answered * 100m / submitted, 2),
            conversionRate = answered == 0 ? 0 : Math.Round(accepted * 100m / answered, 2),
            averageResponseMinutes = responseMinutes.Count == 0 ? 0 : Math.Round(responseMinutes.Average(), 1),
            daily,
            topProducts
        });
    }
}

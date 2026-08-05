using BV.Application.Abstractions.Admin;
using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Queries;

public sealed class AdminDashboardQuery(BVPortalDbContext dbContext) : IAdminDashboardQuery
{
    public async Task<AdminDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalCustomers = await dbContext.CustomerProfiles.CountAsync(cancellationToken);
        var totalQuotes = await dbContext.QuoteRequests.CountAsync(cancellationToken);
        var pendingQuotes = await dbContext.QuoteRequests.CountAsync(
            x => x.Status == QuoteRequestStatus.Submitted || x.Status == QuoteRequestStatus.UnderReview,
            cancellationToken);
        var answeredQuotes = await dbContext.QuoteRequests.CountAsync(
            x => x.Status == QuoteRequestStatus.Answered,
            cancellationToken);
        var failedNotifications = await dbContext.QuoteNotifications.CountAsync(
            x => x.Status == "Failed",
            cancellationToken);

        return new AdminDashboardSummary(
            totalCustomers,
            totalQuotes,
            pendingQuotes,
            answeredQuotes,
            failedNotifications);
    }

    public async Task<IReadOnlyList<AdminDashboardDailyMetric>> GetDailyMetricsAsync(
        int days,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 90);
        var startDate = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var quotes = await dbContext.QuoteRequests
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startDate)
            .Select(x => new { x.CreatedAtUtc, x.SubmittedAtUtc, x.AnsweredAtUtc })
            .ToListAsync(cancellationToken);

        var result = new List<AdminDashboardDailyMetric>(days);
        for (var offset = 0; offset < days; offset++)
        {
            var date = startDate.AddDays(offset);
            var nextDate = date.AddDays(1);

            result.Add(new AdminDashboardDailyMetric(
                DateOnly.FromDateTime(date),
                quotes.Count(x => x.CreatedAtUtc >= date && x.CreatedAtUtc < nextDate),
                quotes.Count(x => x.SubmittedAtUtc >= date && x.SubmittedAtUtc < nextDate),
                quotes.Count(x => x.AnsweredAtUtc >= date && x.AnsweredAtUtc < nextDate)));
        }

        return result;
    }

    public async Task<IReadOnlyList<AdminQuoteListItem>> ListQuotesAsync(
        QuoteRequestStatus? status,
        QuoteRequestType? type,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from quote in dbContext.QuoteRequests.AsNoTracking()
            join customer in dbContext.CustomerProfiles.AsNoTracking()
                on quote.CustomerId equals customer.Id
            select new { quote, customer };

        if (status.HasValue)
            query = query.Where(x => x.quote.Status == status.Value);

        if (type.HasValue)
            query = query.Where(x => x.quote.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.quote.Title.Contains(term) ||
                x.customer.FullName.Contains(term) ||
                x.customer.PhoneNumber.Contains(term) ||
                x.customer.Email.Contains(term));
        }

        return await query
            .OrderByDescending(x => x.quote.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminQuoteListItem(
                x.quote.Id,
                x.quote.Type,
                x.quote.Status,
                x.quote.Title,
                x.customer.FullName,
                x.customer.PhoneNumber,
                x.customer.Email,
                x.quote.Items.Count,
                x.quote.CreatedAtUtc,
                x.quote.SubmittedAtUtc,
                x.quote.AnsweredAtUtc))
            .ToListAsync(cancellationToken);
    }
}

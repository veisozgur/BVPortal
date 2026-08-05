using BV.Domain.Quotes;

namespace BV.Application.Abstractions.Admin;

public interface IAdminDashboardQuery
{
    Task<AdminDashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminQuoteListItem>> ListQuotesAsync(
        QuoteRequestStatus? status,
        QuoteRequestType? type,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardSummary(
    int TotalCustomers,
    int TotalQuoteRequests,
    int PendingQuoteRequests,
    int AnsweredQuoteRequests,
    int FailedNotifications);

public sealed record AdminQuoteListItem(
    Guid Id,
    QuoteRequestType Type,
    QuoteRequestStatus Status,
    string Title,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    int ItemCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? AnsweredAtUtc);

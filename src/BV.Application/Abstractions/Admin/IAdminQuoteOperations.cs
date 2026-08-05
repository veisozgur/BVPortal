using BV.Domain.Quotes;

namespace BV.Application.Abstractions.Admin;

public interface IAdminQuoteOperations
{
    Task<AdminQuoteDetail?> GetDetailAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);
    Task<bool> ChangeStatusAsync(Guid quoteRequestId, QuoteRequestStatus status, CancellationToken cancellationToken = default);
}

public sealed record AdminQuoteDetail(
    Guid Id,
    QuoteRequestType Type,
    QuoteRequestStatus Status,
    string Title,
    string? Description,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string? OrganizationName,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? AnsweredAtUtc,
    IReadOnlyList<AdminQuoteDetailItem> Items,
    AdminQuoteResponseDetail? Response);

public sealed record AdminQuoteDetailItem(Guid Id, string ProductName, decimal Quantity, string Unit, string? Notes);

public sealed record AdminQuoteResponseDetail(
    Guid Id,
    string Message,
    DateTime ValidUntilUtc,
    DateTime? SentAtUtc,
    decimal TotalAmount,
    IReadOnlyList<AdminQuoteResponseItem> Items);

public sealed record AdminQuoteResponseItem(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

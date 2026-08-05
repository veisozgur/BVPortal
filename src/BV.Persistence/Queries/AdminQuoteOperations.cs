using BV.Application.Abstractions.Admin;
using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Queries;

public sealed class AdminQuoteOperations(BVPortalDbContext dbContext) : IAdminQuoteOperations
{
    public async Task<AdminQuoteDetail?> GetDetailAsync(Guid quoteRequestId, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (quote is null) return null;

        var customer = await dbContext.CustomerProfiles
            .AsNoTracking()
            .SingleAsync(x => x.Id == quote.CustomerId, cancellationToken);

        var response = await dbContext.QuoteResponses
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.QuoteRequestId == quoteRequestId, cancellationToken);

        return new AdminQuoteDetail(
            quote.Id, quote.Type, quote.Status, quote.Title, quote.Description,
            customer.FullName, customer.PhoneNumber, customer.Email, customer.OrganizationName,
            quote.CreatedAtUtc, quote.SubmittedAtUtc, quote.AnsweredAtUtc,
            quote.Items.Select(x => new AdminQuoteDetailItem(x.Id, x.ProductName, x.Quantity, x.Unit, x.Notes)).ToList(),
            response is null ? null : new AdminQuoteResponseDetail(
                response.Id, response.Message, response.ValidUntilUtc, response.SentAtUtc, response.TotalAmount,
                response.Items.Select(x => new AdminQuoteResponseItem(
                    x.Id, x.ProductName, x.Quantity, x.Unit, x.UnitPrice, x.VatRate, x.LineTotal)).ToList()));
    }

    public async Task<bool> ChangeStatusAsync(Guid quoteRequestId, QuoteRequestStatus status, CancellationToken cancellationToken = default)
    {
        var quote = await dbContext.QuoteRequests.SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (quote is null) return false;

        switch (status)
        {
            case QuoteRequestStatus.UnderReview:
                quote.StartReview();
                break;
            case QuoteRequestStatus.Cancelled:
                quote.Cancel();
                break;
            default:
                throw new InvalidOperationException("This status cannot be assigned manually.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

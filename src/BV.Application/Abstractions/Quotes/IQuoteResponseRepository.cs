using BV.Domain.Quotes;

namespace BV.Application.Abstractions.Quotes;

public interface IQuoteResponseRepository
{
    Task<QuoteResponse?> GetByRequestIdAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);
    Task AddAsync(QuoteResponse response, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

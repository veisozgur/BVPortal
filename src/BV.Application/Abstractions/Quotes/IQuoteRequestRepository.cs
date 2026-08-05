using BV.Domain.Quotes;

namespace BV.Application.Abstractions.Quotes;

public interface IQuoteRequestRepository
{
    Task AddAsync(QuoteRequest quoteRequest, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuoteRequest>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<QuoteRequest?> GetByIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

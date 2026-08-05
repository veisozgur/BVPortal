using BV.Application.Abstractions.Quotes;
using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class QuoteResponseRepository(BVPortalDbContext dbContext) : IQuoteResponseRepository
{
    public Task<QuoteResponse?> GetByRequestIdAsync(Guid quoteRequestId, CancellationToken cancellationToken = default) =>
        dbContext.QuoteResponses.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.QuoteRequestId == quoteRequestId, cancellationToken);

    public Task AddAsync(QuoteResponse response, CancellationToken cancellationToken = default) =>
        dbContext.QuoteResponses.AddAsync(response, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

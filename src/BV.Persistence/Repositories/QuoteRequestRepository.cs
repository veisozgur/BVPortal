using BV.Application.Abstractions.Quotes;
using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class QuoteRequestRepository(BVPortalDbContext dbContext) : IQuoteRequestRepository
{
    public async Task AddAsync(QuoteRequest quoteRequest, CancellationToken cancellationToken = default) =>
        await dbContext.QuoteRequests.AddAsync(quoteRequest, cancellationToken);

    public async Task<IReadOnlyList<QuoteRequest>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await dbContext.QuoteRequests
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<QuoteRequest?> GetByIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken = default) =>
        dbContext.QuoteRequests
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

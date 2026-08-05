using BV.Application.Abstractions.Customers;
using BV.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class CustomerProfileRepository(BVPortalDbContext dbContext) : ICustomerProfileRepository
{
    public Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.CustomerProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default) =>
        await dbContext.CustomerProfiles.AddAsync(profile, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

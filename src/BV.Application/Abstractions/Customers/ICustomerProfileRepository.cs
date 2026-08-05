using BV.Domain.Customers;

namespace BV.Application.Abstractions.Customers;

public interface ICustomerProfileRepository
{
    Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

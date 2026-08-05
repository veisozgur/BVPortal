using BV.Domain.Users;

namespace BV.Application.Abstractions.Users;

public interface IUserRepository
{
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

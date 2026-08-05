using BV.Application.Abstractions.Users;
using BV.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class UserRepository(BVPortalDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(x => x.Phone == phone, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(x => x.Phone == phone, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

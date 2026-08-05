using BV.Application.Abstractions.Authentication;
using BV.Domain.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class RefreshTokenRepository(BVPortalDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null, cancellationToken);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        => dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}

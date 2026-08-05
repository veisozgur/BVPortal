using BV.Application.Abstractions.Authentication;
using BV.Domain.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BV.Persistence.Repositories;

public sealed class OtpCodeRepository(BVPortalDbContext dbContext) : IOtpCodeRepository
{
    public Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default) =>
        dbContext.OtpCodes.AddAsync(otpCode, cancellationToken).AsTask();

    public Task<OtpCode?> GetLatestActiveAsync(string phone, CancellationToken cancellationToken = default) =>
        dbContext.OtpCodes
            .Where(x => x.Phone == phone && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

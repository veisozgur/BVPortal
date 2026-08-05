using BV.Domain.Authentication;

namespace BV.Application.Abstractions.Authentication;

public interface IOtpCodeRepository
{
    Task AddAsync(OtpCode otpCode, CancellationToken cancellationToken = default);
    Task<OtpCode?> GetLatestActiveAsync(string phone, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

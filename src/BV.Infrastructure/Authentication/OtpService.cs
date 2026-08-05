using System.Security.Cryptography;
using System.Text;
using BV.Application.Abstractions.Authentication;
using BV.Application.Abstractions.Notifications;
using BV.Domain.Authentication;

namespace BV.Infrastructure.Authentication;

public sealed class OtpService(IOtpCodeRepository repository, ISmsSender smsSender) : IOtpService
{
    private const int MaximumAttempts = 3;

    public async Task SendAsync(string phone, CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otp = new OtpCode(phone.Trim(), Hash(code), DateTime.UtcNow.AddMinutes(5));

        await repository.AddAsync(otp, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await smsSender.SendAsync(phone, $"BV Portal doğrulama kodunuz: {code}. Kod 5 dakika geçerlidir.", cancellationToken);
    }

    public async Task<bool> VerifyAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        var otp = await repository.GetLatestActiveAsync(phone.Trim(), cancellationToken);
        if (otp is null || otp.IsExpired(DateTime.UtcNow) || otp.AttemptCount >= MaximumAttempts)
            return false;

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(otp.CodeHash),
                Convert.FromHexString(Hash(code))))
        {
            otp.RegisterFailedAttempt();
            await repository.SaveChangesAsync(cancellationToken);
            return false;
        }

        otp.MarkAsUsed();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

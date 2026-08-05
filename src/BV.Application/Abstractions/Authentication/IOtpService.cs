namespace BV.Application.Abstractions.Authentication;

public interface IOtpService
{
    Task SendAsync(string phone, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(string phone, string code, CancellationToken cancellationToken = default);
}

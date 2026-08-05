using BV.Domain.Common;

namespace BV.Domain.Authentication;

public sealed class OtpCode : BaseEntity
{
    private OtpCode() { }

    public OtpCode(string phone, string codeHash, DateTime expiresAtUtc)
    {
        Phone = phone;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Phone { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public bool IsUsed { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public void RegisterFailedAttempt()
    {
        AttemptCount++;
        MarkUpdated();
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
        MarkUpdated();
    }
}

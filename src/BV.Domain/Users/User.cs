using BV.Domain.Common;

namespace BV.Domain.Users;

public sealed class User : BaseEntity
{
    private User() { }

    public User(string firstName, string lastName, string phone, string email)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? NationalIdentityNumber { get; private set; }
    public bool IsPhoneVerified { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void VerifyPhone()
    {
        IsPhoneVerified = true;
        MarkUpdated();
    }
}

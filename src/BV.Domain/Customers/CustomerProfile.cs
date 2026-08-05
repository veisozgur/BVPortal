namespace BV.Domain.Customers;

public sealed class CustomerProfile
{
    private CustomerProfile() { }

    public CustomerProfile(Guid userId, string fullName, string phoneNumber, string email, string? organizationName, string? taxNumber)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Id = Guid.NewGuid();
        UserId = userId;
        FullName = fullName.Trim();
        PhoneNumber = phoneNumber.Trim();
        Email = email.Trim().ToLowerInvariant();
        OrganizationName = string.IsNullOrWhiteSpace(organizationName) ? null : organizationName.Trim();
        TaxNumber = string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? OrganizationName { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void UpdateAddress(string? address, string? city, string? district)
    {
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

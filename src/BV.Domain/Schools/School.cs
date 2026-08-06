namespace BV.Domain.Schools;

public sealed class School
{
    private School() { }

    public School(string name, string? code, string? contactName, string? phone, string? email, string? address)
    {
        Id = Guid.NewGuid();
        SetDetails(name, code, contactName, phone, email, address);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetDetails(string name, string? code, string? contactName, string? phone, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("School name is required.", nameof(name));

        Name = name.Trim();
        Code = Normalize(code);
        ContactName = Normalize(contactName);
        Phone = Normalize(phone);
        Email = Normalize(email);
        Address = Normalize(address);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

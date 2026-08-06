namespace BV.Domain.Warehouse;

public sealed class Warehouse
{
    private Warehouse() { }

    public Warehouse(string code, string name, string? address)
    {
        Id = Guid.NewGuid();
        SetDetails(code, name, address);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetDetails(string code, string name, string? address)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Warehouse code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

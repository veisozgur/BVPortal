namespace BV.Domain.Warehouse;

public sealed class WarehouseLocation
{
    private WarehouseLocation() { }

    public WarehouseLocation(Guid warehouseId, string code, string name, string? barcode)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));

        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        SetDetails(code, name, barcode);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Barcode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void SetDetails(string code, string name, string? barcode)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Location code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Barcode = string.IsNullOrWhiteSpace(barcode) ? Code : barcode.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

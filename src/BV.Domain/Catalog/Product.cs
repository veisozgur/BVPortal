namespace BV.Domain.Catalog;

public sealed class Product
{
    private Product() { }

    public Product(Guid categoryId, string code, string name, string unit)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("Category is required.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));

        Id = Guid.NewGuid();
        CategoryId = categoryId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Unit = unit.Trim();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal ListPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal StockQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(string name, string? brand, string unit, decimal listPrice, decimal vatRate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (listPrice < 0) throw new ArgumentOutOfRangeException(nameof(listPrice));
        if (vatRate is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(vatRate));

        Name = name.Trim();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        Unit = unit.Trim();
        ListPrice = listPrice;
        VatRate = vatRate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateStock(decimal quantity)
    {
        StockQuantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

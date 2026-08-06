namespace BV.Domain.Warehouse;

public sealed class InventoryBalance
{
    private InventoryBalance() { }

    public InventoryBalance(Guid warehouseId, Guid locationId, Guid productId)
    {
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));
        if (locationId == Guid.Empty) throw new ArgumentException("Location id is required.", nameof(locationId));
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));

        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductId = productId;
        Quantity = 0;
        ReservedQuantity = 0;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => Quantity - ReservedQuantity;
    public DateTime UpdatedAtUtc { get; private set; }

    public void Apply(decimal quantityDelta)
    {
        if (Quantity + quantityDelta < 0)
            throw new InvalidOperationException("Stock quantity cannot be negative.");

        Quantity += quantityDelta;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reserve(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (ReservedQuantity + quantity > Quantity)
            throw new InvalidOperationException("Insufficient available stock.");

        ReservedQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Release(decimal quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Release quantity exceeds reserved stock.");

        ReservedQuantity -= quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

namespace BV.Domain.Warehouse;

public enum StockMovementType
{
    Receipt = 1,
    Issue = 2,
    TransferIn = 3,
    TransferOut = 4,
    CountAdjustment = 5,
    Reservation = 6,
    ReservationRelease = 7
}

public sealed class StockMovement
{
    private StockMovement() { }

    public StockMovement(
        Guid warehouseId,
        Guid locationId,
        Guid productId,
        StockMovementType type,
        decimal quantity,
        string? referenceType,
        Guid? referenceId,
        string? note,
        Guid? createdByUserId)
    {
        if (warehouseId == Guid.Empty) throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));
        if (locationId == Guid.Empty) throw new ArgumentException("Location id is required.", nameof(locationId));
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.NewGuid();
        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        ReferenceType = Normalize(referenceType);
        ReferenceId = referenceId;
        Note = Normalize(note);
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid ProductId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Note { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

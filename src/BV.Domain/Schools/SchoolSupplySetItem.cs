namespace BV.Domain.Schools;

public sealed class SchoolSupplySetItem
{
    private SchoolSupplySetItem() { }

    public SchoolSupplySetItem(Guid supplySetId, Guid? productId, string productName, decimal quantity, string unit, string? note)
    {
        if (supplySetId == Guid.Empty)
            throw new ArgumentException("Supply set id is required.", nameof(supplySetId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));

        Id = Guid.NewGuid();
        SupplySetId = supplySetId;
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid Id { get; private set; }
    public Guid SupplySetId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? Note { get; private set; }
}

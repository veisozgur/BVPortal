namespace BV.Domain.Quotes;

public sealed class QuoteRequestItem
{
    private QuoteRequestItem() { }

    public QuoteRequestItem(string productName, decimal quantity, string unit, string? notes)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));

        Id = Guid.NewGuid();
        ProductName = productName.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid Id { get; private set; }
    public Guid QuoteRequestId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
}

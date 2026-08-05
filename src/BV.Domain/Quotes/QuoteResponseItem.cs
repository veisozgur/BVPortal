namespace BV.Domain.Quotes;

public sealed class QuoteResponseItem
{
    private QuoteResponseItem() { }

    internal QuoteResponseItem(Guid quoteResponseId, string productName, decimal quantity, string unit, decimal unitPrice, decimal vatRate)
    {
        if (quoteResponseId == Guid.Empty)
            throw new ArgumentException("Quote response id is required.", nameof(quoteResponseId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (vatRate < 0 || vatRate > 100)
            throw new ArgumentOutOfRangeException(nameof(vatRate));

        Id = Guid.NewGuid();
        QuoteResponseId = quoteResponseId;
        ProductName = productName.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        UnitPrice = unitPrice;
        VatRate = vatRate;
    }

    public Guid Id { get; private set; }
    public Guid QuoteResponseId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice * (1 + VatRate / 100m);
}

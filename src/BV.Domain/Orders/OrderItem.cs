namespace BV.Domain.Orders;

public sealed class OrderItem
{
    private OrderItem() { }

    public OrderItem(Guid orderId, string productName, decimal quantity, string unit, decimal unitPrice, decimal vatRate)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required.", nameof(unit));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (vatRate is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(vatRate));

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductName = productName.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        UnitPrice = unitPrice;
        VatRate = vatRate;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice * (1 + VatRate / 100m);
}

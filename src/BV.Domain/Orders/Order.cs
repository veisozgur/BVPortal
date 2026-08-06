namespace BV.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public Order(Guid quoteRequestId, Guid quoteResponseId, Guid customerId, string orderNumber)
    {
        if (quoteRequestId == Guid.Empty)
            throw new ArgumentException("Quote request id is required.", nameof(quoteRequestId));
        if (quoteResponseId == Guid.Empty)
            throw new ArgumentException("Quote response id is required.", nameof(quoteResponseId));
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));

        Id = Guid.NewGuid();
        QuoteRequestId = quoteRequestId;
        QuoteResponseId = quoteResponseId;
        CustomerId = customerId;
        OrderNumber = orderNumber.Trim();
        Status = OrderStatus.Created;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteRequestId { get; private set; }
    public Guid QuoteResponseId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public string? CustomerNote { get; private set; }
    public string? InternalNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? ShippedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(x => x.LineTotal);

    public void AddItem(string productName, decimal quantity, string unit, decimal unitPrice, decimal vatRate)
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Order items can only be changed while the order is newly created.");

        _items.Add(new OrderItem(Id, productName, quantity, unit, unitPrice, vatRate));
        Touch();
    }

    public void SetNotes(string? customerNote, string? internalNote)
    {
        CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim();
        InternalNote = string.IsNullOrWhiteSpace(internalNote) ? null : internalNote.Trim();
        Touch();
    }

    public void ChangeStatus(OrderStatus nextStatus)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException("Completed or cancelled orders cannot be changed.");

        var allowed = Status switch
        {
            OrderStatus.Created => nextStatus is OrderStatus.Preparing or OrderStatus.Cancelled,
            OrderStatus.Preparing => nextStatus is OrderStatus.AwaitingSupply or OrderStatus.ReadyForShipment or OrderStatus.Cancelled,
            OrderStatus.AwaitingSupply => nextStatus is OrderStatus.Preparing or OrderStatus.ReadyForShipment or OrderStatus.Cancelled,
            OrderStatus.ReadyForShipment => nextStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => nextStatus is OrderStatus.Completed,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Invalid order status transition: {Status} -> {nextStatus}.");

        Status = nextStatus;
        if (nextStatus == OrderStatus.Shipped)
            ShippedAtUtc = DateTime.UtcNow;
        if (nextStatus == OrderStatus.Completed)
            CompletedAtUtc = DateTime.UtcNow;

        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

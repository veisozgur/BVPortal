namespace BV.Domain.Orders;

public sealed class OrderStatusHistory
{
    private OrderStatusHistory() { }

    public OrderStatusHistory(Guid orderId, OrderStatus fromStatus, OrderStatus toStatus, string? note, Guid? changedByUserId)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id is required.", nameof(orderId));
        if (fromStatus == toStatus)
            throw new ArgumentException("Order status must change.", nameof(toStatus));

        Id = Guid.NewGuid();
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Note { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
}

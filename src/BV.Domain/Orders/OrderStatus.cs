namespace BV.Domain.Orders;

public enum OrderStatus
{
    Created = 1,
    Preparing = 2,
    AwaitingSupply = 3,
    ReadyForShipment = 4,
    Shipped = 5,
    Completed = 6,
    Cancelled = 7
}

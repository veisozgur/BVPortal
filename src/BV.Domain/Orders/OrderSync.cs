namespace BV.Domain.Orders;

public sealed class OrderSync
{
    private OrderSync() { }

    public OrderSync(Guid orderId, string provider)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));

        Id = Guid.NewGuid();
        OrderId = orderId;
        Provider = provider.Trim();
        Status = "Pending";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? ExternalOrderId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public DateTime? LastSuccessAtUtc { get; private set; }

    public void BeginAttempt()
    {
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
        Status = "Processing";
        ErrorMessage = null;
    }

    public void MarkSucceeded(string externalOrderId)
    {
        if (string.IsNullOrWhiteSpace(externalOrderId)) throw new ArgumentException("External order id is required.", nameof(externalOrderId));
        ExternalOrderId = externalOrderId.Trim();
        Status = "Succeeded";
        LastSuccessAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown synchronization error." : errorMessage.Trim();
    }
}

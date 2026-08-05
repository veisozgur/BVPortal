namespace BV.Domain.Notifications;

public sealed class QuoteNotification
{
    private QuoteNotification() { }

    public QuoteNotification(Guid quoteRequestId, string channel, string destination)
    {
        if (quoteRequestId == Guid.Empty)
            throw new ArgumentException("Quote request id is required.", nameof(quoteRequestId));
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel is required.", nameof(channel));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination is required.", nameof(destination));

        Id = Guid.NewGuid();
        QuoteRequestId = quoteRequestId;
        Channel = channel.Trim();
        Destination = destination.Trim();
        Status = "Pending";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteRequestId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public void MarkSent()
    {
        Status = "Sent";
        SentAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
    }
}

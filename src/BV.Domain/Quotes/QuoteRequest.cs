namespace BV.Domain.Quotes;

public sealed class QuoteRequest
{
    private QuoteRequest() { }

    public QuoteRequest(Guid customerId, QuoteRequestType type, string title, string? description)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(customerId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Type = type;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = QuoteRequestStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public QuoteRequestType Type { get; private set; }
    public QuoteRequestStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }

    public void Submit()
    {
        if (Status != QuoteRequestStatus.Draft)
            throw new InvalidOperationException("Only draft quote requests can be submitted.");

        Status = QuoteRequestStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }
}

namespace BV.Domain.Quotes;

public sealed class QuoteResponse
{
    private readonly List<QuoteResponseItem> _items = [];

    private QuoteResponse() { }

    public QuoteResponse(Guid quoteRequestId, string message, DateTime validUntilUtc)
    {
        if (quoteRequestId == Guid.Empty)
            throw new ArgumentException("Quote request id is required.", nameof(quoteRequestId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Response message is required.", nameof(message));
        if (validUntilUtc <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(validUntilUtc), "Validity date must be in the future.");

        Id = Guid.NewGuid();
        QuoteRequestId = quoteRequestId;
        Message = message.Trim();
        ValidUntilUtc = validUntilUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteRequestId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime ValidUntilUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public decimal TotalAmount => _items.Sum(x => x.LineTotal);
    public IReadOnlyCollection<QuoteResponseItem> Items => _items.AsReadOnly();

    public void AddItem(string productName, decimal quantity, string unit, decimal unitPrice, decimal vatRate)
    {
        if (SentAtUtc.HasValue)
            throw new InvalidOperationException("A sent response cannot be changed.");

        _items.Add(new QuoteResponseItem(Id, productName, quantity, unit, unitPrice, vatRate));
    }

    public void MarkAsSent()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("A quote response must contain at least one item.");
        if (SentAtUtc.HasValue)
            throw new InvalidOperationException("The response has already been sent.");

        SentAtUtc = DateTime.UtcNow;
    }
}

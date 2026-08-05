namespace BV.Domain.Quotes;

public sealed class QuoteRequest
{
    private readonly List<QuoteRequestItem> _items = [];

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
    public IReadOnlyCollection<QuoteRequestItem> Items => _items.AsReadOnly();

    public void AddItem(string productName, decimal quantity, string unit, string? notes)
    {
        EnsureDraft();
        _items.Add(new QuoteRequestItem(productName, quantity, unit, notes));
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(x => x.Id == itemId)
            ?? throw new InvalidOperationException("Quote request item was not found.");
        _items.Remove(item);
    }

    public void Submit()
    {
        EnsureDraft();
        if (_items.Count == 0)
            throw new InvalidOperationException("At least one quote request item is required.");

        Status = QuoteRequestStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    private void EnsureDraft()
    {
        if (Status != QuoteRequestStatus.Draft)
            throw new InvalidOperationException("Only draft quote requests can be changed.");
    }
}

namespace BV.Domain.Quotes;

public sealed class QuoteOperationNote
{
    private QuoteOperationNote() { }

    public QuoteOperationNote(Guid quoteRequestId, Guid createdByUserId, string note)
    {
        if (quoteRequestId == Guid.Empty)
            throw new ArgumentException("Quote request id is required.", nameof(quoteRequestId));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Created by user id is required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Note is required.", nameof(note));

        Id = Guid.NewGuid();
        QuoteRequestId = quoteRequestId;
        CreatedByUserId = createdByUserId;
        Note = note.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteRequestId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
}

namespace BV.Domain.Operations;

public sealed class OperationTask
{
    private OperationTask() { }

    public OperationTask(Guid orderId, string title, string? description, OperationTaskPriority priority, DateTime? dueAtUtc)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        Id = Guid.NewGuid();
        OrderId = orderId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        Status = OperationTaskStatus.Open;
        DueAtUtc = dueAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public OperationTaskPriority Priority { get; private set; }
    public OperationTaskStatus Status { get; private set; }
    public DateTime? DueAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public void Assign(Guid? userId)
    {
        AssignedUserId = userId;
        Touch();
    }

    public void Update(string title, string? description, OperationTaskPriority priority, DateTime? dueAtUtc)
    {
        if (Status is OperationTaskStatus.Completed or OperationTaskStatus.Cancelled)
            throw new InvalidOperationException("Closed tasks cannot be edited.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        DueAtUtc = dueAtUtc;
        Touch();
    }

    public void ChangeStatus(OperationTaskStatus nextStatus)
    {
        if (Status is OperationTaskStatus.Completed or OperationTaskStatus.Cancelled)
            throw new InvalidOperationException("Closed tasks cannot be changed.");

        Status = nextStatus;
        CompletedAtUtc = nextStatus == OperationTaskStatus.Completed ? DateTime.UtcNow : null;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

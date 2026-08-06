namespace BV.Domain.Operations;

public enum OperationTaskStatus
{
    Open = 1,
    InProgress = 2,
    Blocked = 3,
    Completed = 4,
    Cancelled = 5
}

public enum OperationTaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

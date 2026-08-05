using BV.Domain.Common;

namespace BV.Domain.Auditing;

public sealed class AuditLog : BaseEntity
{
    private AuditLog() { }

    public AuditLog(Guid? userId, string action, string method, string path, string? ipAddress, int statusCode)
    {
        UserId = userId;
        Action = action;
        Method = method;
        Path = path;
        IpAddress = ipAddress;
        StatusCode = statusCode;
    }

    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Method { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public int StatusCode { get; private set; }
}

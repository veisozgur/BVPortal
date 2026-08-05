using System.Security.Claims;
using BV.Domain.Auditing;
using BV.Persistence;

namespace BV.Api.Middleware;

public sealed class AuditLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, BVPortalDbContext dbContext)
    {
        await next(context);

        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            return;
        }

        Guid? userId = null;
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(subject, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var action = $"{context.Request.Method} {context.Request.Path}";
        var auditLog = new AuditLog(
            userId,
            action,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Response.StatusCode);

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(context.RequestAborted);
    }
}

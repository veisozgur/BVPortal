using BV.Domain.Operations;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/operation-tasks")]
public sealed class AdminOperationTasksController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? orderId,
        [FromQuery] OperationTaskStatus? status,
        [FromQuery] Guid? assignedUserId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OperationTasks.AsNoTracking().AsQueryable();

        if (orderId.HasValue)
            query = query.Where(x => x.OrderId == orderId.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (assignedUserId.HasValue)
            query = query.Where(x => x.AssignedUserId == assignedUserId.Value);

        var items = await query
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueAtUtc)
            .Select(x => new
            {
                x.Id,
                x.OrderId,
                x.AssignedUserId,
                x.Title,
                x.Description,
                x.Priority,
                x.Status,
                x.DueAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOperationTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Orders.AnyAsync(x => x.Id == request.OrderId, cancellationToken))
            return NotFound(new { message = "Sipariş bulunamadı." });

        if (request.AssignedUserId.HasValue &&
            !await dbContext.Users.AnyAsync(x => x.Id == request.AssignedUserId.Value, cancellationToken))
            return BadRequest(new { message = "Atanacak kullanıcı bulunamadı." });

        var task = new OperationTask(request.OrderId, request.Title, request.Description, request.Priority, request.DueAtUtc);
        task.Assign(request.AssignedUserId);

        await dbContext.OperationTasks.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, new { task.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await dbContext.OperationTasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOperationTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.OperationTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
            return NotFound();

        try
        {
            task.Update(request.Title, request.Description, request.Priority, request.DueAtUtc);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Görev güncellendi." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignOperationTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.OperationTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
            return NotFound();

        if (request.UserId.HasValue &&
            !await dbContext.Users.AnyAsync(x => x.Id == request.UserId.Value, cancellationToken))
            return BadRequest(new { message = "Atanacak kullanıcı bulunamadı." });

        task.Assign(request.UserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = request.UserId.HasValue ? "Görev atandı." : "Görev ataması kaldırıldı." });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeOperationTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await dbContext.OperationTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null)
            return NotFound();

        try
        {
            task.ChangeStatus(request.Status);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Görev durumu güncellendi." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

public sealed record CreateOperationTaskRequest(
    Guid OrderId,
    string Title,
    string? Description,
    OperationTaskPriority Priority,
    DateTime? DueAtUtc,
    Guid? AssignedUserId);

public sealed record UpdateOperationTaskRequest(
    string Title,
    string? Description,
    OperationTaskPriority Priority,
    DateTime? DueAtUtc);

public sealed record AssignOperationTaskRequest(Guid? UserId);
public sealed record ChangeOperationTaskStatusRequest(OperationTaskStatus Status);

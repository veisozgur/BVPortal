using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Domain.Quotes;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/quote-requests/{quoteRequestId:guid}/notes")]
public sealed class AdminOperationNotesController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid quoteRequestId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.QuoteRequests.AnyAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (!exists)
            return NotFound();

        var notes = await dbContext.QuoteOperationNotes
            .AsNoTracking()
            .Where(x => x.QuoteRequestId == quoteRequestId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid quoteRequestId,
        [FromBody] CreateOperationNoteRequest request,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.QuoteRequests.AnyAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (!exists)
            return NotFound();

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Yönetici kimliği bulunamadı.");

        if (!Guid.TryParse(userIdValue, out var userId))
            throw new UnauthorizedAccessException("Yönetici kimliği geçersiz.");

        var note = new QuoteOperationNote(quoteRequestId, userId, request.Note);
        await dbContext.QuoteOperationNotes.AddAsync(note, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), new { quoteRequestId }, note);
    }
}

public sealed record CreateOperationNoteRequest(
    [property: Required, MaxLength(2000)] string Note);

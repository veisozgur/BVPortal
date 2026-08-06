using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Application.Abstractions.Customers;
using BV.Application.Abstractions.Quotes;
using BV.Domain.Quotes;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quote-requests/{quoteRequestId:guid}")]
public sealed class CustomerQuoteDecisionsController(
    ICustomerProfileRepository customerProfiles,
    IQuoteRequestRepository quoteRequests,
    BVPortalDbContext dbContext) : ControllerBase
{
    [HttpPost("accept")]
    public Task<IActionResult> Accept(Guid quoteRequestId, CancellationToken cancellationToken)
        => ApplyDecisionAsync(quoteRequestId, static quote => quote.Accept(), cancellationToken);

    [HttpPost("reject")]
    public Task<IActionResult> Reject(Guid quoteRequestId, CancellationToken cancellationToken)
        => ApplyDecisionAsync(quoteRequestId, static quote => quote.Reject(), cancellationToken);

    [HttpPost("revision-request")]
    public async Task<IActionResult> RequestRevision(
        Guid quoteRequestId,
        [FromBody] RevisionRequest request,
        CancellationToken cancellationToken)
    {
        var (profile, userId) = await GetIdentityAsync(cancellationToken);
        var quote = await quoteRequests.GetByIdAsync(quoteRequestId, profile.Id, cancellationToken);
        if (quote is null)
            return NotFound();
        if (quote.Status != QuoteRequestStatus.Answered)
            return Conflict(new { message = "Revizyon yalnızca cevaplanmış teklifler için istenebilir." });

        var note = new QuoteOperationNote(
            quoteRequestId,
            userId,
            $"Müşteri revizyon talebi: {request.Message.Trim()}");

        await dbContext.QuoteOperationNotes.AddAsync(note, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Revizyon talebiniz kaydedildi." });
    }

    private async Task<IActionResult> ApplyDecisionAsync(
        Guid quoteRequestId,
        Action<QuoteRequest> decision,
        CancellationToken cancellationToken)
    {
        var (profile, _) = await GetIdentityAsync(cancellationToken);
        var quote = await quoteRequests.GetByIdAsync(quoteRequestId, profile.Id, cancellationToken);
        if (quote is null)
            return NotFound();

        try
        {
            decision(quote);
            await quoteRequests.SaveChangesAsync(cancellationToken);
            return Ok(new { quote.Id, quote.Status });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private async Task<(BV.Domain.Customers.CustomerProfile Profile, Guid UserId)> GetIdentityAsync(
        CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Kullanıcı kimliği geçersiz.");

        var profile = await customerProfiles.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Müşteri profili bulunamadı.");

        return (profile, userId);
    }
}

public sealed record RevisionRequest(
    [property: Required, MinLength(3), MaxLength(2000)] string Message);

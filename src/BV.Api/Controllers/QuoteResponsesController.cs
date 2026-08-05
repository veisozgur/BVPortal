using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Application.Abstractions.Customers;
using BV.Application.Abstractions.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quote-requests/{quoteRequestId:guid}/response")]
public sealed class QuoteResponsesController(
    ICustomerProfileRepository customerProfiles,
    IQuoteRequestRepository quoteRequests,
    IQuoteResponseRepository quoteResponses) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid quoteRequestId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await customerProfiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return NotFound();

        var quote = await quoteRequests.GetByIdAsync(quoteRequestId, profile.Id, cancellationToken);
        if (quote is null)
            return NotFound();

        var response = await quoteResponses.GetByRequestIdAsync(quoteRequestId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Kullanıcı kimliği geçersiz.");
    }
}

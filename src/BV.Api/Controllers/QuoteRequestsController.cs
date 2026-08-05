using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Application.Abstractions.Customers;
using BV.Application.Abstractions.Quotes;
using BV.Domain.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quote-requests")]
public sealed class QuoteRequestsController(
    ICustomerProfileRepository customerProfiles,
    IQuoteRequestRepository quoteRequests) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        var quotes = await quoteRequests.ListByCustomerAsync(profile.Id, cancellationToken);
        return Ok(quotes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        var quote = await quoteRequests.GetByIdAsync(id, profile.Id, cancellationToken);
        return quote is null ? NotFound() : Ok(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        var quote = new QuoteRequest(profile.Id, request.Type, request.Title, request.Description);

        foreach (var item in request.Items)
            quote.AddItem(item.ProductName, item.Quantity, item.Unit, item.Description);

        if (request.Submit)
            quote.Submit();

        await quoteRequests.AddAsync(quote, cancellationToken);
        await quoteRequests.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = quote.Id }, quote);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);
        var quote = await quoteRequests.GetByIdAsync(id, profile.Id, cancellationToken);
        if (quote is null)
            return NotFound();

        quote.Submit();
        await quoteRequests.SaveChangesAsync(cancellationToken);
        return Ok(quote);
    }

    private async Task<BV.Domain.Customers.CustomerProfile> GetProfileAsync(CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Kullanıcı kimliği geçersiz.");

        return await customerProfiles.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Teklif oluşturmadan önce müşteri profili oluşturulmalıdır.");
    }
}

public sealed record CreateQuoteRequest(
    QuoteRequestType Type,
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(2000)] string? Description,
    [property: MinLength(1)] IReadOnlyCollection<CreateQuoteRequestItem> Items,
    bool Submit = true);

public sealed record CreateQuoteRequestItem(
    [property: Required, MaxLength(300)] string ProductName,
    [property: Range(typeof(decimal), "0.01", "999999999")] decimal Quantity,
    [property: Required, MaxLength(50)] string Unit,
    [property: MaxLength(1000)] string? Description);

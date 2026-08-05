using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Application.Abstractions.Customers;
using BV.Domain.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customer-profile")]
public sealed class CustomerProfilesController(ICustomerProfileRepository profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var profile = await profiles.GetByUserIdAsync(userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (await profiles.GetByUserIdAsync(userId, cancellationToken) is not null)
            return Conflict(new { message = "Müşteri profili zaten mevcut." });

        var profile = new CustomerProfile(
            userId,
            request.FullName,
            request.PhoneNumber,
            request.Email,
            request.OrganizationName,
            request.TaxNumber);

        profile.UpdateAddress(request.Address, request.City, request.District);
        await profiles.AddAsync(profile, cancellationToken);
        await profiles.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { }, profile);
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

public sealed record CreateCustomerProfileRequest(
    [property: Required, MaxLength(200)] string FullName,
    [property: Required, MaxLength(20)] string PhoneNumber,
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: MaxLength(200)] string? OrganizationName,
    [property: MaxLength(20)] string? TaxNumber,
    [property: MaxLength(500)] string? Address,
    [property: MaxLength(100)] string? City,
    [property: MaxLength(100)] string? District);

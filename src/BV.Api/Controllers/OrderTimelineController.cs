using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Application.Abstractions.Customers;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders/{orderId:guid}/timeline")]
public sealed class OrderTimelineController(
    BVPortalDbContext dbContext,
    ICustomerProfileRepository customerProfiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid orderId, CancellationToken cancellationToken)
    {
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(value, out var userId))
                return Unauthorized();

            var profile = await customerProfiles.GetByUserIdAsync(userId, cancellationToken);
            if (profile is null)
                return Forbid();

            var ownsOrder = await dbContext.Orders
                .AsNoTracking()
                .AnyAsync(x => x.Id == orderId && x.CustomerId == profile.Id, cancellationToken);

            if (!ownsOrder)
                return NotFound();
        }
        else if (!await dbContext.Orders.AsNoTracking().AnyAsync(x => x.Id == orderId, cancellationToken))
        {
            return NotFound();
        }

        var items = await dbContext.OrderStatusHistories
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.ChangedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.FromStatus,
                x.ToStatus,
                x.Note,
                changedByUserId = isAdmin ? x.ChangedByUserId : null,
                x.ChangedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}

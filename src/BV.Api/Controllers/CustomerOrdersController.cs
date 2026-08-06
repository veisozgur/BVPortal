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
[Route("api/v1/orders")]
public sealed class CustomerOrdersController(
    BVPortalDbContext dbContext,
    ICustomerProfileRepository customerProfiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(x => x.CustomerId == profile.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.QuoteRequestId,
                x.Status,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.ShippedAtUtc,
                x.CompletedAtUtc,
                itemCount = x.Items.Count,
                totalAmount = x.Items.Sum(item => item.Quantity * item.UnitPrice * (1 + item.VatRate / 100m))
            })
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(cancellationToken);

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId && x.CustomerId == profile.Id, cancellationToken);

        if (order is null)
            return NotFound();

        var timeline = await dbContext.OrderStatusHistories
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.ChangedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.PreviousStatus,
                x.NewStatus,
                x.Note,
                x.ChangedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            order.Id,
            order.OrderNumber,
            order.QuoteRequestId,
            order.Status,
            order.CustomerNote,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.ShippedAtUtc,
            order.CompletedAtUtc,
            order.TotalAmount,
            items = order.Items.Select(item => new
            {
                item.Id,
                item.ProductName,
                item.Quantity,
                item.Unit,
                item.UnitPrice,
                item.VatRate,
                item.LineTotal
            }),
            timeline
        });
    }

    private async Task<BV.Domain.Customers.CustomerProfile> GetProfileAsync(CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("Kullanıcı kimliği geçersiz.");

        return await customerProfiles.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Siparişleri görüntülemek için müşteri profili gereklidir.");
    }
}

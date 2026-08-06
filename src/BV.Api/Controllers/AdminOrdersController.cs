using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BV.Domain.Orders;
using BV.Domain.Quotes;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/orders")]
public sealed class AdminOrdersController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] OrderStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Orders.AsNoTracking();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.QuoteRequestId,
                x.CustomerId,
                x.Status,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.ShippedAtUtc,
                x.CompletedAtUtc,
                totalAmount = x.Items.Sum(item => item.Quantity * item.UnitPrice * (1 + item.VatRate / 100m)),
                itemCount = x.Items.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items
        });
    }

    [HttpPost("from-quote/{quoteRequestId:guid}")]
    public async Task<IActionResult> CreateFromQuote(
        Guid quoteRequestId,
        [FromBody] CreateOrderFromQuoteRequest? request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Orders.AnyAsync(x => x.QuoteRequestId == quoteRequestId, cancellationToken))
            return Conflict(new { message = "Bu teklif daha önce siparişe dönüştürüldü." });

        var quote = await dbContext.QuoteRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);

        if (quote is null)
            return NotFound(new { message = "Teklif talebi bulunamadı." });

        if (quote.Status != QuoteRequestStatus.Accepted)
            return Conflict(new { message = "Yalnızca kabul edilmiş teklifler siparişe dönüştürülebilir." });

        var response = await dbContext.QuoteResponses
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.QuoteRequestId == quoteRequestId, cancellationToken);

        if (response is null || response.Items.Count == 0)
            return Conflict(new { message = "Teklif fiyat cevabı veya kalemleri bulunamadı." });

        var order = new Order(
            quote.Id,
            response.Id,
            quote.CustomerId,
            GenerateOrderNumber());

        foreach (var item in response.Items)
            order.AddItem(item.ProductName, item.Quantity, item.Unit, item.UnitPrice, item.VatRate);

        order.SetNotes(request?.CustomerNote, request?.InternalNote);

        await dbContext.Orders.AddAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { orderId = order.Id }, new
        {
            order.Id,
            order.OrderNumber,
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc
        });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(new
        {
            order.Id,
            order.OrderNumber,
            order.QuoteRequestId,
            order.QuoteResponseId,
            order.CustomerId,
            order.Status,
            order.CustomerNote,
            order.InternalNote,
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
            })
        });
    }

    [HttpPut("{orderId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid orderId,
        [FromBody] ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        var previousStatus = order.Status;

        try
        {
            order.ChangeStatus(request.Status);
            await dbContext.OrderStatusHistories.AddAsync(
                new OrderStatusHistory(order.Id, previousStatus, order.Status, request.Note, GetActorUserId()),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }

        return Ok(new
        {
            order.Id,
            order.OrderNumber,
            order.Status,
            order.UpdatedAtUtc,
            order.ShippedAtUtc,
            order.CompletedAtUtc
        });
    }

    [HttpPut("{orderId:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(
        Guid orderId,
        [FromBody] UpdateOrderNotesRequest request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        order.SetNotes(request.CustomerNote, request.InternalNote);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Sipariş notları güncellendi." });
    }

    private Guid? GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static string GenerateOrderNumber()
        => $"BV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}

public sealed record CreateOrderFromQuoteRequest(string? CustomerNote, string? InternalNote);
public sealed record ChangeOrderStatusRequest(OrderStatus Status, string? Note);
public sealed record UpdateOrderNotesRequest(string? CustomerNote, string? InternalNote);

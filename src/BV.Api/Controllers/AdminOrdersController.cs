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

    private static string GenerateOrderNumber()
        => $"BV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}

public sealed record CreateOrderFromQuoteRequest(string? CustomerNote, string? InternalNote);

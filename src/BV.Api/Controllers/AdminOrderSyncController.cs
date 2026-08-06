using System.Net.Http.Json;
using BV.Domain.Orders;
using BV.Infrastructure.Integrations;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/orders/{orderId:guid}/sync")]
public sealed class AdminOrderSyncController(
    BVPortalDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<MikroOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStatus(Guid orderId, CancellationToken cancellationToken)
    {
        var sync = await dbContext.OrderSyncs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId && x.Provider == "Mikro", cancellationToken);

        return sync is null
            ? Ok(new { orderId, provider = "Mikro", status = "NotStarted", attemptCount = 0 })
            : Ok(new
            {
                sync.OrderId,
                sync.Provider,
                sync.Status,
                sync.ExternalOrderId,
                sync.ErrorMessage,
                sync.AttemptCount,
                sync.LastAttemptAtUtc,
                sync.LastSuccessAtUtc
            });
    }

    [HttpPost]
    public async Task<IActionResult> Synchronize(Guid orderId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
            return Conflict(new { message = "Mikro Bridge yapılandırması hazır değil." });

        var order = await dbContext.Orders.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null) return NotFound(new { message = "Sipariş bulunamadı." });

        var sync = await dbContext.OrderSyncs
            .SingleOrDefaultAsync(x => x.OrderId == orderId && x.Provider == "Mikro", cancellationToken);
        if (sync?.Status == "Succeeded")
            return Conflict(new { message = "Sipariş daha önce Mikro'ya aktarıldı.", sync.ExternalOrderId });

        sync ??= new OrderSync(order.Id, "Mikro");
        if (dbContext.Entry(sync).State == EntityState.Detached)
            await dbContext.OrderSyncs.AddAsync(sync, cancellationToken);

        sync.BeginAttempt();
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var client = httpClientFactory.CreateClient("MikroBridge");
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);

            var payload = new
            {
                companyCode = settings.CompanyCode,
                orderNumber = order.OrderNumber,
                customerId = order.CustomerId,
                orderDateUtc = order.CreatedAtUtc,
                customerNote = order.CustomerNote,
                items = order.Items.Select(x => new
                {
                    x.ProductName,
                    x.Quantity,
                    x.Unit,
                    x.UnitPrice,
                    x.VatRate,
                    x.LineTotal
                })
            };

            var response = await client.PostAsJsonAsync(settings.OrdersPath, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Mikro Bridge HTTP {(int)response.StatusCode} döndürdü.");

            var result = await response.Content.ReadFromJsonAsync<MikroOrderSyncResponse>(cancellationToken: cancellationToken);
            if (result is null || string.IsNullOrWhiteSpace(result.ExternalOrderId))
                throw new InvalidOperationException("Mikro Bridge geçerli sipariş numarası döndürmedi.");

            sync.MarkSucceeded(result.ExternalOrderId);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Sipariş Mikro'ya aktarıldı.", sync.ExternalOrderId, sync.LastSuccessAtUtc });
        }
        catch (Exception ex)
        {
            sync.MarkFailed(ex.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Mikro senkronizasyonu başarısız.", error = ex.Message });
        }
    }
}

public sealed record MikroOrderSyncResponse(string ExternalOrderId);

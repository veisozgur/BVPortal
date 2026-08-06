using System.Net.Http.Json;
using BV.Domain.Catalog;
using BV.Infrastructure.Integrations;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/integrations/mikro")]
public sealed class AdminMikroSyncController(
    IHttpClientFactory httpClientFactory,
    IOptions<MikroOptions> options,
    BVPortalDbContext dbContext) : ControllerBase
{
    [HttpPost("sync-catalog")]
    public async Task<IActionResult> SyncCatalog(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return Conflict(new { message = "Mikro entegrasyonu devre dışı." });

        if (string.IsNullOrWhiteSpace(settings.BaseUrl) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.CompanyCode))
            return UnprocessableEntity(new { message = "Mikro Bridge yapılandırması eksik." });

        var client = httpClientFactory.CreateClient("MikroBridge");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{settings.ProductsPath.TrimStart('/')}?companyCode={Uri.EscapeDataString(settings.CompanyCode)}");
        request.Headers.TryAddWithoutValidation("X-Api-Key", settings.ApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = $"Mikro Bridge ürün verisi alınamadı ({(int)response.StatusCode})."
            });

        var incoming = await response.Content.ReadFromJsonAsync<List<MikroProductDto>>(cancellationToken: cancellationToken) ?? [];
        if (incoming.Count == 0)
            return Ok(new MikroSyncResult(0, 0, 0, 0));

        var categories = await dbContext.ProductCategories.ToListAsync(cancellationToken);
        var products = await dbContext.Products.ToListAsync(cancellationToken);
        var categoryByName = categories.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var productByCode = products.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var createdCategories = 0;
        var createdProducts = 0;
        var updatedProducts = 0;
        var skipped = 0;

        foreach (var row in incoming)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || string.IsNullOrWhiteSpace(row.Name))
            {
                skipped++;
                continue;
            }

            var categoryName = string.IsNullOrWhiteSpace(row.Category) ? "Genel" : row.Category.Trim();
            if (!categoryByName.TryGetValue(categoryName, out var category))
            {
                category = new ProductCategory(categoryName);
                dbContext.ProductCategories.Add(category);
                categoryByName[categoryName] = category;
                createdCategories++;
            }

            var normalizedCode = row.Code.Trim().ToUpperInvariant();
            if (!productByCode.TryGetValue(normalizedCode, out var product))
            {
                product = new Product(category.Id, normalizedCode, row.Name, string.IsNullOrWhiteSpace(row.Unit) ? "Adet" : row.Unit);
                product.Update(row.Name, row.Brand, string.IsNullOrWhiteSpace(row.Unit) ? "Adet" : row.Unit, Math.Max(0, row.ListPrice), Math.Clamp(row.VatRate, 0, 100));
                product.UpdateStock(row.StockQuantity);
                dbContext.Products.Add(product);
                productByCode[normalizedCode] = product;
                createdProducts++;
            }
            else
            {
                product.Update(row.Name, row.Brand, string.IsNullOrWhiteSpace(row.Unit) ? product.Unit : row.Unit, Math.Max(0, row.ListPrice), Math.Clamp(row.VatRate, 0, 100));
                product.UpdateStock(row.StockQuantity);
                updatedProducts++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new MikroSyncResult(createdCategories, createdProducts, updatedProducts, skipped));
    }
}

public sealed record MikroProductDto(
    string Code,
    string Name,
    string? Category,
    string? Brand,
    string? Unit,
    decimal ListPrice,
    decimal VatRate,
    decimal StockQuantity);

public sealed record MikroSyncResult(
    int CreatedCategories,
    int CreatedProducts,
    int UpdatedProducts,
    int SkippedRows);

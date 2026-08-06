using System.ComponentModel.DataAnnotations;
using BV.Domain.Catalog;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/catalog")]
public sealed class AdminCatalogController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var items = await dbContext.ProductCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description, x.IsActive, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.ProductCategories.AnyAsync(x => x.Name == request.Name.Trim(), cancellationToken))
            return Conflict(new { message = "Bu kategori zaten kayıtlı." });

        var category = new ProductCategory(request.Name, request.Description);
        dbContext.ProductCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(ListCategories), new { id = category.Id }, category);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return NotFound();
        category.Update(request.Name, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(category);
    }

    [HttpGet("products")]
    public async Task<IActionResult> ListProducts([FromQuery] string? search, [FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term) || (x.Brand != null && x.Brand.Contains(term)));
        }

        var items = await query.OrderBy(x => x.Name).Take(500).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(ProductRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.ProductCategories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken))
            return BadRequest(new { message = "Kategori bulunamadı." });
        if (await dbContext.Products.AnyAsync(x => x.Code == request.Code.Trim().ToUpper(), cancellationToken))
            return Conflict(new { message = "Bu ürün kodu zaten kayıtlı." });

        var product = new Product(request.CategoryId, request.Code, request.Name, request.Unit);
        product.Update(request.Name, request.Brand, request.Unit, request.ListPrice, request.VatRate);
        product.UpdateStock(request.StockQuantity);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(ListProducts), new { id = product.Id }, product);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return NotFound();
        product.Update(request.Name, request.Brand, request.Unit, request.ListPrice, request.VatRate);
        product.UpdateStock(request.StockQuantity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(product);
    }

    public sealed record CategoryRequest(
        [property: Required, MaxLength(150)] string Name,
        [property: MaxLength(1000)] string? Description);

    public sealed record ProductRequest(
        Guid CategoryId,
        [property: Required, MaxLength(80)] string Code,
        [property: Required, MaxLength(250)] string Name,
        [property: MaxLength(150)] string? Brand,
        [property: Required, MaxLength(50)] string Unit,
        decimal ListPrice,
        decimal VatRate,
        decimal StockQuantity);
}

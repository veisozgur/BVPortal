using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/customers")]
public sealed class AdminCustomersController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.CustomerProfiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.PhoneNumber.Contains(term) ||
                x.Email.Contains(term) ||
                (x.OrganizationName != null && x.OrganizationName.Contains(term)));
        }

        var customers = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.FullName,
                x.PhoneNumber,
                x.Email,
                x.OrganizationName,
                x.City,
                x.District,
                QuoteCount = dbContext.QuoteRequests.Count(q => q.CustomerId == x.Id),
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.FullName,
                x.PhoneNumber,
                x.Email,
                x.OrganizationName,
                x.TaxNumber,
                x.Address,
                x.City,
                x.District,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                Quotes = dbContext.QuoteRequests
                    .Where(q => q.CustomerId == x.Id)
                    .OrderByDescending(q => q.CreatedAtUtc)
                    .Select(q => new { q.Id, q.Title, q.Type, q.Status, q.CreatedAtUtc })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return customer is null ? NotFound() : Ok(customer);
    }
}

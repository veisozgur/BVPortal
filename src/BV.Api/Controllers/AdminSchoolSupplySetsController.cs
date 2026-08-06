using BV.Domain.Schools;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/supply-sets")]
public sealed class AdminSchoolSupplySetsController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? schoolId,
        [FromQuery] Guid? gradeId,
        [FromQuery] int? academicYear,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolSupplySets.AsNoTracking();

        if (schoolId.HasValue)
            query = query.Where(x => x.SchoolId == schoolId.Value);
        if (gradeId.HasValue)
            query = query.Where(x => x.SchoolGradeId == gradeId.Value);
        if (academicYear.HasValue)
            query = query.Where(x => x.AcademicYear == academicYear.Value);
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var sets = await query
            .OrderByDescending(x => x.AcademicYear)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.SchoolId,
                schoolName = dbContext.Schools.Where(s => s.Id == x.SchoolId).Select(s => s.Name).FirstOrDefault(),
                x.SchoolGradeId,
                gradeName = dbContext.SchoolGrades.Where(g => g.Id == x.SchoolGradeId).Select(g => g.Name).FirstOrDefault(),
                x.Name,
                x.AcademicYear,
                x.Description,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                itemCount = x.Items.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(sets);
    }

    [HttpGet("{setId:guid}")]
    public async Task<IActionResult> GetById(Guid setId, CancellationToken cancellationToken)
    {
        var set = await dbContext.SchoolSupplySets
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == setId, cancellationToken);

        if (set is null)
            return NotFound(new { message = "Okul seti bulunamadı." });

        var school = await dbContext.Schools.AsNoTracking()
            .Where(x => x.Id == set.SchoolId)
            .Select(x => new { x.Id, x.Name })
            .SingleAsync(cancellationToken);

        var grade = await dbContext.SchoolGrades.AsNoTracking()
            .Where(x => x.Id == set.SchoolGradeId)
            .Select(x => new { x.Id, x.Name })
            .SingleAsync(cancellationToken);

        return Ok(new
        {
            set.Id,
            school,
            grade,
            set.Name,
            set.AcademicYear,
            set.Description,
            set.IsActive,
            set.CreatedAtUtc,
            set.UpdatedAtUtc,
            items = set.Items.OrderBy(x => x.ProductName).Select(x => new
            {
                x.Id,
                x.ProductId,
                x.ProductName,
                x.Quantity,
                x.Unit,
                x.Note
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveSchoolSupplySetRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateSchoolAndGrade(request.SchoolId, request.SchoolGradeId, cancellationToken);
        if (validation is not null)
            return validation;

        if (await dbContext.SchoolSupplySets.AnyAsync(
                x => x.SchoolId == request.SchoolId &&
                     x.SchoolGradeId == request.SchoolGradeId &&
                     x.AcademicYear == request.AcademicYear,
                cancellationToken))
        {
            return Conflict(new { message = "Bu okul, sınıf ve eğitim yılı için zaten bir set bulunuyor." });
        }

        try
        {
            var set = new SchoolSupplySet(request.SchoolId, request.SchoolGradeId, request.Name, request.AcademicYear);
            set.Update(request.Name, request.Description, request.IsActive);

            foreach (var item in request.Items ?? [])
            {
                var itemValidation = await ValidateProduct(item.ProductId, cancellationToken);
                if (itemValidation is not null)
                    return itemValidation;

                set.AddItem(item.ProductId, item.ProductName, item.Quantity, item.Unit, item.Note);
            }

            await dbContext.SchoolSupplySets.AddAsync(set, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { setId = set.Id }, new
            {
                set.Id,
                set.Name,
                set.AcademicYear,
                set.IsActive,
                itemCount = set.Items.Count
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{setId:guid}")]
    public async Task<IActionResult> Update(
        Guid setId,
        [FromBody] UpdateSchoolSupplySetRequest request,
        CancellationToken cancellationToken)
    {
        var set = await dbContext.SchoolSupplySets.SingleOrDefaultAsync(x => x.Id == setId, cancellationToken);
        if (set is null)
            return NotFound(new { message = "Okul seti bulunamadı." });

        try
        {
            set.Update(request.Name, request.Description, request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        return Ok(new { message = "Okul seti güncellendi." });
    }

    [HttpPost("{setId:guid}/items")]
    public async Task<IActionResult> AddItem(
        Guid setId,
        [FromBody] SaveSchoolSupplySetItemRequest request,
        CancellationToken cancellationToken)
    {
        var set = await dbContext.SchoolSupplySets.SingleOrDefaultAsync(x => x.Id == setId, cancellationToken);
        if (set is null)
            return NotFound(new { message = "Okul seti bulunamadı." });

        var itemValidation = await ValidateProduct(request.ProductId, cancellationToken);
        if (itemValidation is not null)
            return itemValidation;

        try
        {
            set.AddItem(request.ProductId, request.ProductName, request.Quantity, request.Unit, request.Note);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        return Ok(new { message = "Set kalemi eklendi." });
    }

    [HttpDelete("{setId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid setId, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await dbContext.SchoolSupplySetItems
            .SingleOrDefaultAsync(x => x.Id == itemId && x.SupplySetId == setId, cancellationToken);

        if (item is null)
            return NotFound(new { message = "Set kalemi bulunamadı." });

        dbContext.SchoolSupplySetItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Set kalemi silindi." });
    }

    [HttpPost("{setId:guid}/copy")]
    public async Task<IActionResult> CopyToAcademicYear(
        Guid setId,
        [FromBody] CopySchoolSupplySetRequest request,
        CancellationToken cancellationToken)
    {
        var source = await dbContext.SchoolSupplySets
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == setId, cancellationToken);

        if (source is null)
            return NotFound(new { message = "Kaynak okul seti bulunamadı." });

        if (await dbContext.SchoolSupplySets.AnyAsync(
                x => x.SchoolId == source.SchoolId &&
                     x.SchoolGradeId == source.SchoolGradeId &&
                     x.AcademicYear == request.AcademicYear,
                cancellationToken))
        {
            return Conflict(new { message = "Hedef eğitim yılı için zaten bir set bulunuyor." });
        }

        try
        {
            var copy = new SchoolSupplySet(
                source.SchoolId,
                source.SchoolGradeId,
                string.IsNullOrWhiteSpace(request.Name) ? source.Name : request.Name,
                request.AcademicYear);

            copy.Update(copy.Name, source.Description, true);
            foreach (var item in source.Items)
                copy.AddItem(item.ProductId, item.ProductName, item.Quantity, item.Unit, item.Note);

            await dbContext.SchoolSupplySets.AddAsync(copy, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetById), new { setId = copy.Id }, new
            {
                copy.Id,
                copy.Name,
                copy.AcademicYear,
                itemCount = copy.Items.Count
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private async Task<IActionResult?> ValidateSchoolAndGrade(
        Guid schoolId,
        Guid gradeId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Schools.AnyAsync(x => x.Id == schoolId, cancellationToken))
            return NotFound(new { message = "Okul bulunamadı." });

        if (!await dbContext.SchoolGrades.AnyAsync(
                x => x.Id == gradeId && x.SchoolId == schoolId,
                cancellationToken))
        {
            return BadRequest(new { message = "Sınıf/kademe seçilen okula ait değil." });
        }

        return null;
    }

    private async Task<IActionResult?> ValidateProduct(Guid? productId, CancellationToken cancellationToken)
    {
        if (productId.HasValue && !await dbContext.Products.AnyAsync(x => x.Id == productId.Value, cancellationToken))
            return BadRequest(new { message = "Seçilen katalog ürünü bulunamadı." });

        return null;
    }
}

public sealed record SaveSchoolSupplySetRequest(
    Guid SchoolId,
    Guid SchoolGradeId,
    string Name,
    int AcademicYear,
    string? Description,
    bool IsActive = true,
    IReadOnlyList<SaveSchoolSupplySetItemRequest>? Items = null);

public sealed record UpdateSchoolSupplySetRequest(string Name, string? Description, bool IsActive = true);

public sealed record SaveSchoolSupplySetItemRequest(
    Guid? ProductId,
    string ProductName,
    decimal Quantity,
    string Unit,
    string? Note);

public sealed record CopySchoolSupplySetRequest(int AcademicYear, string? Name);

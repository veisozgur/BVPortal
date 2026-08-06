using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/school-supply-sets")]
public sealed class SchoolSupplySetCatalogController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet("schools")]
    public async Task<IActionResult> Schools(CancellationToken cancellationToken)
    {
        var schools = await dbContext.Schools
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToListAsync(cancellationToken);

        return Ok(schools);
    }

    [HttpGet("schools/{schoolId:guid}/grades")]
    public async Task<IActionResult> Grades(Guid schoolId, CancellationToken cancellationToken)
    {
        var grades = await dbContext.SchoolGrades
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        return Ok(grades);
    }

    [HttpGet]
    public async Task<IActionResult> Sets(
        [FromQuery] Guid schoolId,
        [FromQuery] Guid gradeId,
        [FromQuery] int? academicYear,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolSupplySets
            .AsNoTracking()
            .Where(x => x.IsActive && x.SchoolId == schoolId && x.SchoolGradeId == gradeId);

        if (academicYear.HasValue)
            query = query.Where(x => x.AcademicYear == academicYear.Value);

        var sets = await query
            .OrderByDescending(x => x.AcademicYear)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.AcademicYear,
                x.Description,
                itemCount = x.Items.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(sets);
    }

    [HttpGet("{setId:guid}")]
    public async Task<IActionResult> Get(Guid setId, CancellationToken cancellationToken)
    {
        var set = await dbContext.SchoolSupplySets
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == setId && x.IsActive, cancellationToken);

        if (set is null)
            return NotFound(new { message = "Aktif okul seti bulunamadı." });

        return Ok(new
        {
            set.Id,
            set.SchoolId,
            set.SchoolGradeId,
            set.Name,
            set.AcademicYear,
            set.Description,
            items = set.Items
                .OrderBy(x => x.ProductName)
                .Select(x => new
                {
                    x.ProductName,
                    x.Quantity,
                    x.Unit,
                    description = x.Note
                })
        });
    }
}

using BV.Domain.Schools;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/schools")]
public sealed class AdminSchoolsController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Schools.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Code != null && x.Code.Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        var schools = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.ContactName,
                x.Phone,
                x.Email,
                x.Address,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                gradeCount = dbContext.SchoolGrades.Count(g => g.SchoolId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return Ok(schools);
    }

    [HttpGet("{schoolId:guid}")]
    public async Task<IActionResult> GetById(Guid schoolId, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == schoolId, cancellationToken);

        if (school is null)
            return NotFound(new { message = "Okul bulunamadı." });

        var grades = await dbContext.SchoolGrades
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            school.Id,
            school.Name,
            school.Code,
            school.ContactName,
            school.Phone,
            school.Email,
            school.Address,
            school.IsActive,
            school.CreatedAtUtc,
            school.UpdatedAtUtc,
            grades = grades.Select(x => new
            {
                x.Id,
                x.Name,
                x.SortOrder,
                x.IsActive,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSchoolRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Okul adı zorunludur." });

        var normalizedCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        if (normalizedCode is not null && await dbContext.Schools.AnyAsync(x => x.Code == normalizedCode, cancellationToken))
            return Conflict(new { message = "Bu okul kodu daha önce kullanılmış." });

        var school = new School(
            request.Name,
            normalizedCode,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address);

        school.SetActive(request.IsActive);
        await dbContext.Schools.AddAsync(school, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { schoolId = school.Id }, new
        {
            school.Id,
            school.Name,
            school.Code,
            school.IsActive
        });
    }

    [HttpPut("{schoolId:guid}")]
    public async Task<IActionResult> Update(
        Guid schoolId,
        [FromBody] SaveSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools.SingleOrDefaultAsync(x => x.Id == schoolId, cancellationToken);
        if (school is null)
            return NotFound(new { message = "Okul bulunamadı." });

        var normalizedCode = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        if (normalizedCode is not null && await dbContext.Schools.AnyAsync(
                x => x.Id != schoolId && x.Code == normalizedCode,
                cancellationToken))
        {
            return Conflict(new { message = "Bu okul kodu başka bir okul tarafından kullanılıyor." });
        }

        try
        {
            school.SetDetails(request.Name, normalizedCode, request.ContactName, request.Phone, request.Email, request.Address);
            school.SetActive(request.IsActive);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        return Ok(new { message = "Okul güncellendi." });
    }

    [HttpPost("{schoolId:guid}/grades")]
    public async Task<IActionResult> CreateGrade(
        Guid schoolId,
        [FromBody] SaveSchoolGradeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Schools.AnyAsync(x => x.Id == schoolId, cancellationToken))
            return NotFound(new { message = "Okul bulunamadı." });

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Sınıf/kademe adı zorunludur." });

        if (await dbContext.SchoolGrades.AnyAsync(x => x.SchoolId == schoolId && x.Name == name, cancellationToken))
            return Conflict(new { message = "Bu sınıf/kademe okulda zaten tanımlı." });

        var grade = new SchoolGrade(schoolId, name, request.SortOrder);
        grade.Update(name, request.SortOrder, request.IsActive);
        await dbContext.SchoolGrades.AddAsync(grade, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { grade.Id, grade.Name, grade.SortOrder, grade.IsActive });
    }

    [HttpPut("{schoolId:guid}/grades/{gradeId:guid}")]
    public async Task<IActionResult> UpdateGrade(
        Guid schoolId,
        Guid gradeId,
        [FromBody] SaveSchoolGradeRequest request,
        CancellationToken cancellationToken)
    {
        var grade = await dbContext.SchoolGrades
            .SingleOrDefaultAsync(x => x.Id == gradeId && x.SchoolId == schoolId, cancellationToken);

        if (grade is null)
            return NotFound(new { message = "Sınıf/kademe bulunamadı." });

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Sınıf/kademe adı zorunludur." });

        if (await dbContext.SchoolGrades.AnyAsync(
                x => x.SchoolId == schoolId && x.Id != gradeId && x.Name == name,
                cancellationToken))
        {
            return Conflict(new { message = "Bu sınıf/kademe okulda zaten tanımlı." });
        }

        grade.Update(name, request.SortOrder, request.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Sınıf/kademe güncellendi." });
    }
}

public sealed record SaveSchoolRequest(
    string Name,
    string? Code,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive = true);

public sealed record SaveSchoolGradeRequest(string Name, int SortOrder, bool IsActive = true);

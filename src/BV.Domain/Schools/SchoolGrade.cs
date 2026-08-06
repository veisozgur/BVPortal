namespace BV.Domain.Schools;

public sealed class SchoolGrade
{
    private SchoolGrade() { }

    public SchoolGrade(Guid schoolId, string name, int sortOrder)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("School id is required.", nameof(schoolId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Grade name is required.", nameof(name));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        Name = name.Trim();
        SortOrder = Math.Max(0, sortOrder);
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(string name, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Grade name is required.", nameof(name));

        Name = name.Trim();
        SortOrder = Math.Max(0, sortOrder);
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

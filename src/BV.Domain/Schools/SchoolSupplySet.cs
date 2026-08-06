namespace BV.Domain.Schools;

public sealed class SchoolSupplySet
{
    private readonly List<SchoolSupplySetItem> _items = [];

    private SchoolSupplySet() { }

    public SchoolSupplySet(Guid schoolId, Guid schoolGradeId, string name, int academicYear)
    {
        if (schoolId == Guid.Empty)
            throw new ArgumentException("School id is required.", nameof(schoolId));
        if (schoolGradeId == Guid.Empty)
            throw new ArgumentException("School grade id is required.", nameof(schoolGradeId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Set name is required.", nameof(name));
        if (academicYear < 2020 || academicYear > 2100)
            throw new ArgumentOutOfRangeException(nameof(academicYear));

        Id = Guid.NewGuid();
        SchoolId = schoolId;
        SchoolGradeId = schoolGradeId;
        Name = name.Trim();
        AcademicYear = academicYear;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid SchoolGradeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int AcademicYear { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<SchoolSupplySetItem> Items => _items.AsReadOnly();

    public void Update(string name, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Set name is required.", nameof(name));

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddItem(Guid? productId, string productName, decimal quantity, string unit, string? note)
    {
        _items.Add(new SchoolSupplySetItem(Id, productId, productName, quantity, unit, note));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

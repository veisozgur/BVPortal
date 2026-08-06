using BV.Domain.Schools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class SchoolGradeConfiguration : IEntityTypeConfiguration<SchoolGrade>
{
    public void Configure(EntityTypeBuilder<SchoolGrade> builder)
    {
        builder.ToTable("SchoolGrades");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.SchoolId, x.SortOrder });
        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

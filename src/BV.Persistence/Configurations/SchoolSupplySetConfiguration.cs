using BV.Domain.Schools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class SchoolSupplySetConfiguration : IEntityTypeConfiguration<SchoolSupplySet>
{
    public void Configure(EntityTypeBuilder<SchoolSupplySet> builder)
    {
        builder.ToTable("SchoolSupplySets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.SchoolId, x.SchoolGradeId, x.AcademicYear }).IsUnique();
        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolGrade>()
            .WithMany()
            .HasForeignKey(x => x.SchoolGradeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.SupplySetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

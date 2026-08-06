using BV.Domain.Catalog;
using BV.Domain.Schools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class SchoolSupplySetItemConfiguration : IEntityTypeConfiguration<SchoolSupplySetItem>
{
    public void Configure(EntityTypeBuilder<SchoolSupplySetItem> builder)
    {
        builder.ToTable("SchoolSupplySetItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => x.SupplySetId);
        builder.HasIndex(x => x.ProductId);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

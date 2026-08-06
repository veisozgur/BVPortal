using BV.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.QuoteRequestId).IsUnique();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CustomerNote).HasMaxLength(2000);
        builder.Property(x => x.InternalNote).HasMaxLength(2000);
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

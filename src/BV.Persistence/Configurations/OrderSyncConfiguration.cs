using BV.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class OrderSyncConfiguration : IEntityTypeConfiguration<OrderSync>
{
    public void Configure(EntityTypeBuilder<OrderSync> builder)
    {
        builder.ToTable("OrderSyncs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalOrderId).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => new { x.OrderId, x.Provider }).IsUnique();
    }
}

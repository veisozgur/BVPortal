using BV.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class OperationTaskConfiguration : IEntityTypeConfiguration<OperationTask>
{
    public void Configure(EntityTypeBuilder<OperationTask> builder)
    {
        builder.ToTable("OperationTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Priority).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.AssignedUserId);
        builder.HasIndex(x => new { x.Status, x.DueAtUtc });
        builder.HasOne<BV.Domain.Orders.Order>()
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<BV.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

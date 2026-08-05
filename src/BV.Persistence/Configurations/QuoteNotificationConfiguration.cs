using BV.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class QuoteNotificationConfiguration : IEntityTypeConfiguration<QuoteNotification>
{
    public void Configure(EntityTypeBuilder<QuoteNotification> builder)
    {
        builder.ToTable("QuoteNotifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Destination).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(x => x.QuoteRequestId);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}

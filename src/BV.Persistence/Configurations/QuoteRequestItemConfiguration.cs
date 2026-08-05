using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class QuoteRequestItemConfiguration : IEntityTypeConfiguration<QuoteRequestItem>
{
    public void Configure(EntityTypeBuilder<QuoteRequestItem> builder)
    {
        builder.ToTable("QuoteRequestItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => x.QuoteRequestId);
    }
}

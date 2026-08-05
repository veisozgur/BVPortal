using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class QuoteResponseItemConfiguration : IEntityTypeConfiguration<QuoteResponseItem>
{
    public void Configure(EntityTypeBuilder<QuoteResponseItem> builder)
    {
        builder.ToTable("QuoteResponseItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.VatRate).HasPrecision(5, 2);
        builder.Ignore(x => x.LineTotal);
        builder.HasIndex(x => x.QuoteResponseId);
    }
}

using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class QuoteResponseConfiguration : IEntityTypeConfiguration<QuoteResponse>
{
    public void Configure(EntityTypeBuilder<QuoteResponse> builder)
    {
        builder.ToTable("QuoteResponses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Ignore(x => x.TotalAmount);

        builder.HasIndex(x => x.QuoteRequestId).IsUnique();
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.QuoteResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

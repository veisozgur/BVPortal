using BV.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BV.Persistence.Configurations;

public sealed class QuoteOperationNoteConfiguration : IEntityTypeConfiguration<QuoteOperationNote>
{
    public void Configure(EntityTypeBuilder<QuoteOperationNote> builder)
    {
        builder.ToTable("QuoteOperationNotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.QuoteRequestId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

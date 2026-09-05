using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class DeckVersionEntryConfiguration : IEntityTypeConfiguration<DeckVersionEntry>
{
    public void Configure(EntityTypeBuilder<DeckVersionEntry> builder)
    {
        builder.ToTable("deck_version_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.DeckVersionId).HasColumnName("deck_version_id");
        builder.Property(e => e.CardId).HasColumnName("card_id");
        builder.Property(e => e.Quantity).HasColumnName("quantity");
        builder.Property(e => e.Section).HasColumnName("section").HasConversion<int>();
    }
}

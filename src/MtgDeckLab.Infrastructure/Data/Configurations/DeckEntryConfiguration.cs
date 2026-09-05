using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class DeckEntryConfiguration : IEntityTypeConfiguration<DeckEntry>
{
    public void Configure(EntityTypeBuilder<DeckEntry> builder)
    {
        builder.ToTable("deck_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.DeckId).HasColumnName("deck_id");
        builder.Property(e => e.CardId).HasColumnName("card_id");
        builder.Property(e => e.Quantity).HasColumnName("quantity");
        builder.Property(e => e.Section).HasColumnName("section").HasConversion<int>();

        builder.HasIndex(e => new { e.DeckId, e.CardId, e.Section }).IsUnique();
    }
}

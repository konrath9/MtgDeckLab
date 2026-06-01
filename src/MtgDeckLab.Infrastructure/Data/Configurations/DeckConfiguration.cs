using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class DeckConfiguration : IEntityTypeConfiguration<Deck>
{
    public void Configure(EntityTypeBuilder<Deck> builder)
    {
        builder.ToTable("decks");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.UserId).HasColumnName("user_id");
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(d => d.Format).HasColumnName("format").HasConversion<int>();
        builder.Property(d => d.Description).HasColumnName("description");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");

        // Computed read-only views — EF must not try to map these
        builder.Ignore(d => d.MainDeck);
        builder.Ignore(d => d.Sideboard);
        builder.Ignore(d => d.CommanderSlot);
        builder.Ignore(d => d.TotalMainDeckCards);
        builder.Ignore(d => d.TotalSideboardCards);

        // EF auto-detects _entries as the backing field for Entries by naming convention
        builder.HasMany(d => d.Entries)
            .WithOne()
            .HasForeignKey(e => e.DeckId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

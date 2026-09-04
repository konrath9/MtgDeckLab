using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class DeckVersionConfiguration : IEntityTypeConfiguration<DeckVersion>
{
    public void Configure(EntityTypeBuilder<DeckVersion> builder)
    {
        builder.ToTable("deck_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(v => v.DeckId).HasColumnName("deck_id");
        builder.Property(v => v.VersionNumber).HasColumnName("version_number");
        builder.Property(v => v.Score).HasColumnName("score");
        builder.Property(v => v.Grade).HasColumnName("grade").HasMaxLength(2).IsRequired();
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");

        // Computed read-only views — EF must not try to map these
        builder.Ignore(v => v.TotalMainDeckCards);
        builder.Ignore(v => v.TotalSideboardCards);

        builder.HasIndex(v => new { v.DeckId, v.VersionNumber }).IsUnique();

        // Sem navigation property em Deck (histórico é um conceito à parte, como FinanceSnapshot),
        // mas com FK + cascade de verdade — diferente de FinanceSnapshot, que exige limpeza manual.
        builder.HasOne<Deck>()
            .WithMany()
            .HasForeignKey(v => v.DeckId)
            .OnDelete(DeleteBehavior.Cascade);

        // EF auto-detects _entries as the backing field for Entries by naming convention
        builder.HasMany(v => v.Entries)
            .WithOne()
            .HasForeignKey(e => e.DeckVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

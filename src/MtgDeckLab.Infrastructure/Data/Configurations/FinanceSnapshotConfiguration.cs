using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class FinanceSnapshotConfiguration : IEntityTypeConfiguration<FinanceSnapshot>
{
    public void Configure(EntityTypeBuilder<FinanceSnapshot> builder)
    {
        builder.ToTable("finance_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.DeckId).HasColumnName("deck_id");
        builder.Property(s => s.TotalCostUsd).HasColumnName("total_cost_usd").HasPrecision(12, 2);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(s => s.DeckId);
    }
}

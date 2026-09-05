using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class CardLocalizedNameConfiguration : IEntityTypeConfiguration<CardLocalizedName>
{
    public void Configure(EntityTypeBuilder<CardLocalizedName> builder)
    {
        builder.ToTable("card_localized_names");

        // Uma carta tem no máximo um nome por idioma — a chave composta é a própria regra.
        builder.HasKey(n => new { n.CardId, n.Language });

        builder.Property(n => n.CardId).HasColumnName("card_id");
        builder.Property(n => n.Language).HasColumnName("language").HasMaxLength(8).IsRequired();
        builder.Property(n => n.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(n => n.PrintedTypeLine).HasColumnName("printed_type_line").HasMaxLength(256);
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at");

        // Busca de carta casa contra nome traduzido tanto quanto contra o inglês; sem este índice
        // toda busca por nome vira sequential scan em cima de ~30k linhas por idioma.
        builder.HasIndex(n => n.Name);
    }
}

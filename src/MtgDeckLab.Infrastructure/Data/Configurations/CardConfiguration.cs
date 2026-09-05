using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Infrastructure.Data.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    private static readonly JsonSerializerOptions JsonOpts = new();

    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.HasIndex(c => c.ScryfallId).IsUnique();

        builder.Property(c => c.ScryfallId).HasColumnName("scryfall_id").IsRequired();
        builder.Property(c => c.OracleId).HasColumnName("oracle_id").IsRequired();
        builder.HasIndex(c => c.OracleId);
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
        builder.Property(c => c.ManaCost).HasColumnName("mana_cost").HasMaxLength(128);
        // precision 10 — alguns cards de Un-sets têm cmc absurdo (ex.: Gleemax = 1.000.000).
        builder.Property(c => c.Cmc).HasColumnName("cmc").HasPrecision(10, 2);
        builder.Property(c => c.TypeLine).HasColumnName("type_line").HasMaxLength(256).IsRequired();
        builder.Property(c => c.OracleText).HasColumnName("oracle_text");
        builder.Property(c => c.Power).HasColumnName("power").HasMaxLength(8);
        builder.Property(c => c.Toughness).HasColumnName("toughness").HasMaxLength(8);
        builder.Property(c => c.Loyalty).HasColumnName("loyalty").HasMaxLength(8);
        builder.Property(c => c.PriceUsd).HasColumnName("price_usd").HasPrecision(10, 2);
        builder.Property(c => c.PriceUsdFoil).HasColumnName("price_usd_foil").HasPrecision(10, 2);
        builder.Property(c => c.SetCode).HasColumnName("set_code").HasMaxLength(8).IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Colors/ColorIdentity mapeados como integer[] nativo do Postgres (não JSON-as-text) para
        // que EF.Property(...).Contains(...) seja traduzido para "= ANY(coluna)" em SQL — permite
        // filtrar por cor em CardRepository.SearchAsync.
        builder.PrimitiveCollection<List<Color>>("_colors")
            .HasColumnName("colors")
            .HasColumnType("integer[]")
            .ElementType().HasConversion<int>();

        builder.PrimitiveCollection<List<Color>>("_colorIdentity")
            .HasColumnName("color_identity")
            .HasColumnType("integer[]")
            .ElementType().HasConversion<int>();

        builder.Property<List<CardSuperType>>("_supertypes")
            .HasColumnName("supertypes")
            .HasConversion(EnumListConverter<CardSuperType>(), EnumListComparer<CardSuperType>());

        builder.Property<List<CardType>>("_types")
            .HasColumnName("types")
            .HasConversion(EnumListConverter<CardType>(), EnumListComparer<CardType>());

        builder.Property<List<string>>("_subtypes")
            .HasColumnName("subtypes")
            .HasConversion(StringListConverter(), StringListComparer());

        // EF acha _localizedNames como backing field de LocalizedNames por convenção.
        builder.HasMany(c => c.LocalizedNames)
            .WithOne()
            .HasForeignKey(n => n.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<T>, string> EnumListConverter<T>()
        where T : struct, Enum =>
        new(
            v => JsonSerializer.Serialize(v.Select(e => Convert.ToInt32(e)).ToList(), JsonOpts),
            v => JsonSerializer.Deserialize<List<int>>(v, JsonOpts)!.Select(i => (T)(object)i).ToList()
        );

    private static ValueComparer<List<T>> EnumListComparer<T>() where T : struct, Enum =>
        new(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
            c => c.ToList()
        );

    private static Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string> StringListConverter() =>
        new(
            v => JsonSerializer.Serialize(v, JsonOpts),
            v => JsonSerializer.Deserialize<List<string>>(v, JsonOpts) ?? new List<string>()
        );

    private static ValueComparer<List<string>> StringListComparer() =>
        new(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
            c => c.ToList()
        );
}

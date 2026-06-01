using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class Card
{
    private readonly List<Color> _colors;
    private readonly List<Color> _colorIdentity;
    private readonly List<CardSuperType> _supertypes;
    private readonly List<CardType> _types;
    private readonly List<string> _subtypes;

    public Guid Id { get; private init; }
    public Guid ScryfallId { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public string? ManaCost { get; private init; }
    public decimal Cmc { get; private init; }
    public IReadOnlyList<Color> Colors => _colors.AsReadOnly();
    public IReadOnlyList<Color> ColorIdentity => _colorIdentity.AsReadOnly();
    public string TypeLine { get; private init; } = string.Empty;
    public IReadOnlyList<CardSuperType> Supertypes => _supertypes.AsReadOnly();
    public IReadOnlyList<CardType> Types => _types.AsReadOnly();
    public IReadOnlyList<string> Subtypes => _subtypes.AsReadOnly();
    public string? OracleText { get; private init; }
    public string? Power { get; private init; }
    public string? Toughness { get; private init; }
    public string? Loyalty { get; private init; }
    public decimal? PriceUsd { get; private set; }
    public decimal? PriceUsdFoil { get; private set; }
    public string SetCode { get; private init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    // For EF Core — backing field access configured in Infrastructure
    private Card()
    {
        _colors = new();
        _colorIdentity = new();
        _supertypes = new();
        _types = new();
        _subtypes = new();
    }

    public Card(
        Guid scryfallId,
        string name,
        string? manaCost,
        decimal cmc,
        IEnumerable<Color> colors,
        IEnumerable<Color> colorIdentity,
        string typeLine,
        IEnumerable<CardSuperType> supertypes,
        IEnumerable<CardType> types,
        IEnumerable<string> subtypes,
        string? oracleText,
        string? power,
        string? toughness,
        string? loyalty,
        decimal? priceUsd,
        decimal? priceUsdFoil,
        string setCode)
    {
        Id = Guid.NewGuid();
        ScryfallId = scryfallId;
        Name = name;
        ManaCost = manaCost;
        Cmc = cmc;
        _colors = colors.ToList();
        _colorIdentity = colorIdentity.ToList();
        TypeLine = typeLine;
        _supertypes = supertypes.ToList();
        _types = types.ToList();
        _subtypes = subtypes.ToList();
        OracleText = oracleText;
        Power = power;
        Toughness = toughness;
        Loyalty = loyalty;
        PriceUsd = priceUsd;
        PriceUsdFoil = priceUsdFoil;
        SetCode = setCode;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePrices(decimal? priceUsd, decimal? priceUsdFoil)
    {
        PriceUsd = priceUsd;
        PriceUsdFoil = priceUsdFoil;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsLand => _types.Contains(CardType.Land);
    public bool IsCreature => _types.Contains(CardType.Creature);
    public bool IsLegendary => _supertypes.Contains(CardSuperType.Legendary);
}

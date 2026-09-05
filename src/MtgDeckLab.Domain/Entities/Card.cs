using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Domain.Localization;

namespace MtgDeckLab.Domain.Entities;

public sealed class Card
{
    private readonly List<Color> _colors;
    private readonly List<Color> _colorIdentity;
    private readonly List<CardSuperType> _supertypes;
    private readonly List<CardType> _types;
    private readonly List<string> _subtypes;
    private readonly List<CardLocalizedName> _localizedNames;

    public Guid Id { get; private init; }
    public Guid ScryfallId { get; private init; }

    // Identidade "oracle" da carta na Scryfall: estável entre reimpressões E entre idiomas, e por
    // isso a chave usada pra casar as traduções com esta linha. ScryfallId identifica uma
    // impressão específica (muda a cada set), então não serve pra isso.
    public Guid OracleId { get; private set; }

    /// <summary>Nome canônico em inglês. Nomes em outros idiomas ficam em <see cref="LocalizedNames"/>.</summary>
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
    public IReadOnlyList<CardLocalizedName> LocalizedNames => _localizedNames.AsReadOnly();

    // For EF Core — backing field access configured in Infrastructure
    private Card()
    {
        _colors = new();
        _colorIdentity = new();
        _supertypes = new();
        _types = new();
        _subtypes = new();
        _localizedNames = new();
    }

    public Card(
        Guid scryfallId,
        Guid oracleId,
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
        OracleId = oracleId;
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
        _localizedNames = new();
    }

    public void UpdatePrices(decimal? priceUsd, decimal? priceUsdFoil)
    {
        PriceUsd = priceUsd;
        PriceUsdFoil = priceUsdFoil;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Preenche o oracle id em linhas gravadas antes de ele existir. Só avança de "vazio" pra um id
    // real — nunca reescreve um id válido, que é justamente o que amarra as traduções já
    // sincronizadas a esta carta.
    public void SyncOracleId(Guid oracleId)
    {
        if (oracleId == Guid.Empty || OracleId != Guid.Empty) return;

        OracleId = oracleId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Insere ou atualiza o nome desta carta em um idioma.</summary>
    public void SetLocalizedName(string language, string name, string? printedTypeLine = null)
    {
        var normalized = CardLanguage.Normalize(language);

        // Inglês é o nome canônico da própria carta — guardá-lo aqui de novo criaria duas fontes
        // de verdade pro mesmo dado.
        if (normalized == CardLanguage.English) return;

        var existing = _localizedNames.FirstOrDefault(n => n.Language == normalized);
        if (existing is not null)
            existing.Update(name, printedTypeLine);
        else
            _localizedNames.Add(new CardLocalizedName(Id, normalized, name, printedTypeLine));
    }

    /// <summary>
    /// Nome da carta no idioma pedido, caindo pro nome canônico em inglês quando não há tradução —
    /// a UI nunca fica sem nome pra mostrar.
    /// </summary>
    public string NameIn(string? language)
    {
        var normalized = CardLanguage.Normalize(language);
        if (normalized == CardLanguage.English) return Name;

        return _localizedNames.FirstOrDefault(n => n.Language == normalized)?.Name ?? Name;
    }

    public bool IsLand => _types.Contains(CardType.Land);
    public bool IsCreature => _types.Contains(CardType.Creature);
    public bool IsLegendary => _supertypes.Contains(CardSuperType.Legendary);
}

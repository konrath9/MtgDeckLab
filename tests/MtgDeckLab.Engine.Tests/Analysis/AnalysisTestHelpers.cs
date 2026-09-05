using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

internal static class AnalysisTestHelpers
{
    public static DeckAnalysisEntry MakeEntry(
        string name = "Test Card",
        decimal cmc = 2,
        int quantity = 1,
        CardType[]? types = null,
        CardSuperType[]? supertypes = null,
        Color[]? colors = null,
        Color[]? colorIdentity = null,
        DeckSection section = DeckSection.Main,
        string? oracleText = null) =>
        new(
            CardName: name,
            Cmc: cmc,
            Colors: (colors ?? [Color.Red]).AsReadOnly(),
            ColorIdentity: (colorIdentity ?? colors ?? [Color.Red]).AsReadOnly(),
            Types: (types ?? [CardType.Instant]).AsReadOnly(),
            Supertypes: (supertypes ?? []).AsReadOnly(),
            Quantity: quantity,
            Section: section,
            OracleText: oracleText
        );

    public static DeckAnalysisEntry Land(string name = "Forest", int quantity = 1, bool isBasic = true) =>
        new(
            CardName: name,
            Cmc: 0,
            Colors: Array.AsReadOnly(Array.Empty<Color>()),
            ColorIdentity: [Color.Green],
            Types: [CardType.Land],
            Supertypes: isBasic ? [CardSuperType.Basic] : [],
            Quantity: quantity,
            Section: DeckSection.Main
        );

    public static DeckAnalysisEntry BasicLand(Color identity, int quantity = 1)
    {
        var name = identity switch
        {
            Color.White => "Plains",
            Color.Blue => "Island",
            Color.Black => "Swamp",
            Color.Red => "Mountain",
            Color.Green => "Forest",
            _ => "Wastes"
        };
        return new(name, 0,
            Array.AsReadOnly(Array.Empty<Color>()),
            identity == Color.Colorless ? Array.AsReadOnly(Array.Empty<Color>()) : new Color[] { identity }.AsReadOnly(),
            [CardType.Land], [CardSuperType.Basic],
            quantity, DeckSection.Main);
    }

    public static DeckAnalysisEntry Colorless(string name = "Sol Ring", decimal cmc = 1) =>
        new(name, cmc,
            Array.AsReadOnly(Array.Empty<Color>()),
            Array.AsReadOnly(Array.Empty<Color>()),
            [CardType.Artifact], [], 1, DeckSection.Main);

    public static DeckAnalysisEntry Creature(
        string name = "Test Creature",
        decimal cmc = 3,
        int quantity = 1,
        Color[]? colors = null) =>
        MakeEntry(name, cmc, quantity, [CardType.Creature], colors: colors ?? [Color.Green]);

    public static DeckAnalysisEntry Commander(
        string name = "Atraxa",
        decimal cmc = 4,
        Color[]? colors = null) =>
        new(
            CardName: name,
            Cmc: cmc,
            Colors: (colors ?? [Color.White, Color.Blue, Color.Black, Color.Green]).AsReadOnly(),
            ColorIdentity: (colors ?? [Color.White, Color.Blue, Color.Black, Color.Green]).AsReadOnly(),
            Types: [CardType.Creature],
            Supertypes: [CardSuperType.Legendary],
            Quantity: 1,
            Section: DeckSection.Commander
        );

    public static DeckForAnalysis CommanderDeck(
        IEnumerable<DeckAnalysisEntry> mainDeck,
        DeckAnalysisEntry? commander = null) =>
        new("Test Deck", Format.Commander,
            (commander is not null
                ? mainDeck.Append(commander)
                : mainDeck.Append(Commander())));

    public static DeckForAnalysis ConstructedDeck(
        IEnumerable<DeckAnalysisEntry> mainDeck,
        Format format = Format.Modern) =>
        new("Test Deck", format, mainDeck);
}

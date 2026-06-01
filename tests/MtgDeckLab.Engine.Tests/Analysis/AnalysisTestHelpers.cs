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
        bool isCommander = false,
        bool isSideboard = false) =>
        new(
            CardName: name,
            Cmc: cmc,
            Colors: (colors ?? [Color.Red]).AsReadOnly(),
            ColorIdentity: (colorIdentity ?? colors ?? [Color.Red]).AsReadOnly(),
            Types: (types ?? [CardType.Instant]).AsReadOnly(),
            Supertypes: (supertypes ?? []).AsReadOnly(),
            Quantity: quantity,
            IsCommander: isCommander,
            IsSideboard: isSideboard
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
            IsCommander: false,
            IsSideboard: false
        );

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
            IsCommander: true,
            IsSideboard: false
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

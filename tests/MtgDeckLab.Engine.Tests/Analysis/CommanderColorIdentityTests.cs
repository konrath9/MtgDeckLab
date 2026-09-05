using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class CommanderColorIdentityTests
{
    // Cria 99 cards mono-Red de preenchimento para um deck Commander válido em contagem
    private static IEnumerable<DeckAnalysisEntry> RedFiller(int count = 62, int lands = 36) =>
        Enumerable.Range(1, count)
            .Select(i => AnalysisTestHelpers.Creature($"Goblin {i}", colors: [Color.Red]))
            .Append(AnalysisTestHelpers.BasicLand(Color.Red, lands));

    [Fact]
    public void CardOutsideCommanderIdentity_ReturnsColorIdentityError()
    {
        var blueCard = AnalysisTestHelpers.MakeEntry(
            "Counterspell", cmc: 2, types: [CardType.Instant],
            colors: [Color.Blue], colorIdentity: [Color.Blue]);

        var mainDeck = RedFiller().Append(blueCard);
        var deck = AnalysisTestHelpers.CommanderDeck(
            mainDeck,
            AnalysisTestHelpers.Commander("Purphoros", colors: [Color.Red]));

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().Contain(e =>
            e.Is(AnalysisMessageCodes.CommanderColorIdentity, "Counterspell"));
    }

    [Fact]
    public void AllCardsWithinCommanderIdentity_NoColorIdentityError()
    {
        // Atraxa (WUBG) — todas as cartas são mono-Green
        var mainDeck = Enumerable.Range(1, 61)
            .Select(i => AnalysisTestHelpers.Creature($"Elf {i}", colors: [Color.Green]))
            .Append(AnalysisTestHelpers.BasicLand(Color.Green, 38));

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().NotContain(e => e.Is(AnalysisMessageCodes.CommanderColorIdentity));
    }

    [Fact]
    public void ColorlessCard_IsAlwaysValidRegardlessOfCommanderIdentity()
    {
        var solRing = AnalysisTestHelpers.Colorless("Sol Ring");

        var mainDeck = RedFiller(61, 36).Append(solRing);
        var deck = AnalysisTestHelpers.CommanderDeck(
            mainDeck,
            AnalysisTestHelpers.Commander("Purphoros", colors: [Color.Red]));

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().NotContain(e => e.MentionsCard("Sol Ring"));
    }

    [Fact]
    public void MultipleViolatingCards_EachReportedSeparately()
    {
        var island = AnalysisTestHelpers.BasicLand(Color.Blue);
        var swamp = AnalysisTestHelpers.BasicLand(Color.Black);

        var mainDeck = RedFiller(60, 35).Append(island).Append(swamp);
        var deck = AnalysisTestHelpers.CommanderDeck(
            mainDeck,
            AnalysisTestHelpers.Commander("Purphoros", colors: [Color.Red]));

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderColorIdentity, "Island"));
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderColorIdentity, "Swamp"));
    }

    [Fact]
    public void PartnerCommanders_UnionOfIdentitiesIsUsed()
    {
        // Partner 1: Red, Partner 2: Green → identity = {Red, Green}
        var redPartner = new DeckAnalysisEntry(
            "Rograkh", 0,
            [Color.Red], [Color.Red],
            [CardType.Creature], [CardSuperType.Legendary],
            1, DeckSection.Commander);

        var greenPartner = new DeckAnalysisEntry(
            "Sakashima", 2,
            [Color.Green], [Color.Green],
            [CardType.Creature], [CardSuperType.Legendary],
            1, DeckSection.Commander);

        // Carta Blue deve ser inválida (Blue ∉ {Red, Green})
        var blueCard = AnalysisTestHelpers.MakeEntry(
            "Brainstorm", cmc: 1, types: [CardType.Instant],
            colors: [Color.Blue], colorIdentity: [Color.Blue]);

        // Carta Red-Green deve ser válida
        var gruulCard = AnalysisTestHelpers.MakeEntry(
            "Bloodbraid Elf", cmc: 4, types: [CardType.Creature],
            colors: [Color.Red, Color.Green], colorIdentity: [Color.Red, Color.Green]);

        var mainDeck = Enumerable.Range(1, 59)
            .Select(i => AnalysisTestHelpers.Creature($"Goblin {i}", colors: [Color.Red]))
            .Append(AnalysisTestHelpers.BasicLand(Color.Red, 36))
            .Append(blueCard)
            .Append(gruulCard);

        // Deck com dois comandantes
        var deck = new DeckForAnalysis("Partner Deck", Format.Commander,
            mainDeck.Append(redPartner).Append(greenPartner));

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderColorIdentity, "Brainstorm"));
        result.Errors.Should().NotContain(e => e.MentionsCard("Bloodbraid Elf"));
    }

    [Fact]
    public void NoCommanderDesignated_ColorIdentityCheckIsSkipped()
    {
        // Deck sem #Commander — não deve gerar erro de color identity
        var blueCard = AnalysisTestHelpers.MakeEntry(
            "Counterspell", cmc: 2, types: [CardType.Instant],
            colors: [Color.Blue], colorIdentity: [Color.Blue]);

        var mainDeck = RedFiller(62, 36).Append(blueCard);

        // CommanderDeck sem commander — usa o default (Atraxa WUBG) mas vamos forçar sem commander
        var deck = new DeckForAnalysis("No Commander", Format.Commander, mainDeck);

        var result = FormatValidator.Validate(deck);

        // Deve ter warning de sem comandante, mas NÃO erro de color identity
        result.Warnings.Should().Contain(w => w.Is(AnalysisMessageCodes.CommanderMissing));
        result.Errors.Should().NotContain(e => e.Is(AnalysisMessageCodes.CommanderColorIdentity));
    }
}

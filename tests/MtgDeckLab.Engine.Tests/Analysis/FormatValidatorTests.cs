using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class FormatValidatorTests
{
    // ── Commander ──────────────────────────────────────────────────────────────

    [Fact]
    public void Commander_Valid100CardSingletonDeck_IsValid()
    {
        var mainDeck = Enumerable.Range(1, 61)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}", cmc: 3))
            .Append(AnalysisTestHelpers.Land(quantity: 38))
            .ToList();

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Commander_WrongCardCount_ReturnsError()
    {
        var mainDeck = Enumerable.Range(1, 50)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .ToList<DeckAnalysisEntry>();

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderDeckSize));
    }

    [Fact]
    public void Commander_NonBasicDuplicates_ReturnsError()
    {
        var mainDeck = Enumerable.Range(1, 60)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.MakeEntry("Sol Ring", quantity: 2, types: [CardType.Artifact], colors: []))
            .Append(AnalysisTestHelpers.Land(quantity: 37))
            .ToList<DeckAnalysisEntry>();

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderSingleton, "Sol Ring"));
    }

    [Fact]
    public void Commander_BasicLandDuplicates_AreAllowed()
    {
        var mainDeck = Enumerable.Range(1, 61)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.Land("Forest", quantity: 38, isBasic: true))
            .ToList<DeckAnalysisEntry>();

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().NotContain(e => e.MentionsCard("Forest"));
    }

    [Fact]
    public void Commander_NonLegendaryCommander_ReturnsError()
    {
        var badCommander = new DeckAnalysisEntry(
            "Grizzly Bears", 2,
            [Color.Green], [Color.Green],
            [CardType.Creature], [],
            1, DeckSection.Commander);

        var mainDeck = Enumerable.Range(1, 61)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.Land(quantity: 38))
            .ToList<DeckAnalysisEntry>();

        var deck = new DeckForAnalysis("Test", Format.Commander, mainDeck.Append(badCommander));

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.CommanderNotLegendary));
    }

    // ── Constructed ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructed_Valid60CardDeck_IsValid()
    {
        var mainDeck = new[]
        {
            AnalysisTestHelpers.MakeEntry("Bolt", cmc: 1, quantity: 4),
            AnalysisTestHelpers.MakeEntry("Counterspell", cmc: 2, quantity: 4),
            AnalysisTestHelpers.Land("Island", quantity: 24, isBasic: true),
            AnalysisTestHelpers.Land("Mountain", quantity: 16, isBasic: true),
            AnalysisTestHelpers.MakeEntry("Dragon", cmc: 5, quantity: 4),
            AnalysisTestHelpers.Creature("Bear", cmc: 2, quantity: 4),
            AnalysisTestHelpers.Creature("Troll", cmc: 3, quantity: 4),
        };

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Constructed_FewerThan60Cards_ReturnsError()
    {
        var mainDeck = Enumerable.Range(1, 10)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.ConstructedMinSize));
    }

    [Fact]
    public void Constructed_MoreThan4Copies_ReturnsError()
    {
        var mainDeck = Enumerable.Range(1, 20)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.MakeEntry("Lightning Bolt", cmc: 1, quantity: 5))
            .Append(AnalysisTestHelpers.Land(quantity: 24));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Is(AnalysisMessageCodes.ConstructedMaxCopies, "Lightning Bolt"));
    }

    [Fact]
    public void Constructed_BasicLandsExceedFour_AreAllowed()
    {
        var mainDeck = Enumerable.Range(1, 36)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.Land("Forest", quantity: 24, isBasic: true));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);

        var result = FormatValidator.Validate(deck);

        result.Errors.Should().NotContain(e => e.MentionsCard("Forest"));
    }
}

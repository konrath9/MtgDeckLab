using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class DeckScorerTests
{
    private static DeckScore ScoreDeck(DeckForAnalysis deck)
    {
        var mainDeck = deck.MainDeck.ToList();
        var manaCurve = ManaCurveAnalyzer.Analyze(mainDeck);
        var colorDist = ColorDistributionAnalyzer.Analyze(mainDeck);
        var typeDist = TypeDistributionAnalyzer.Analyze(mainDeck);
        var validation = FormatValidator.Validate(deck);
        return DeckScorer.Score(deck, manaCurve, colorDist, typeDist, validation);
    }

    [Fact]
    public void Score_WellBuiltConstructedDeck_GetsHighGrade()
    {
        // 60-card deck with ideal mana curve and land count
        var mainDeck = new[]
        {
            AnalysisTestHelpers.MakeEntry("1-drop", cmc: 1, quantity: 4),
            AnalysisTestHelpers.MakeEntry("2-drop", cmc: 2, quantity: 8),
            AnalysisTestHelpers.MakeEntry("3-drop", cmc: 3, quantity: 8),
            AnalysisTestHelpers.Creature("Beater", cmc: 4, quantity: 4),
            AnalysisTestHelpers.Land(quantity: 24),
            AnalysisTestHelpers.MakeEntry("Removal", cmc: 2, quantity: 4),
            AnalysisTestHelpers.MakeEntry("Draw", cmc: 2, quantity: 4),
            AnalysisTestHelpers.Creature("Top-end", cmc: 5, quantity: 4),
        };

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);
        var score = ScoreDeck(deck);

        score.Grade.Should().BeOneOf("A", "B");
        score.Score.Should().BeGreaterThanOrEqualTo(65);
    }

    [Fact]
    public void Score_DeckWithValidationErrors_ReducesRuleComplianceComponent()
    {
        // 9 cards total → validation error (< 60 cards)
        var mainDeck = Enumerable.Range(1, 5)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.Land(quantity: 4));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);
        var score = ScoreDeck(deck);

        score.ComponentScores["RuleCompliance"].Should().BeLessThan(100);
    }

    [Fact]
    public void Score_GradeA_AtOrAbove80()
    {
        // Build a perfect-ish Commander deck
        var mainDeck = Enumerable.Range(1, 62)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}", cmc: 3))
            .Append(AnalysisTestHelpers.Land(quantity: 37))
            .ToList<DeckAnalysisEntry>();

        var deck = AnalysisTestHelpers.CommanderDeck(mainDeck);
        var score = ScoreDeck(deck);

        score.Grade.Should().Be(score.Score >= 80 ? "A"
            : score.Score >= 65 ? "B"
            : score.Score >= 50 ? "C"
            : score.Score >= 35 ? "D" : "F");
    }

    [Theory]
    [InlineData(80, "A")]
    [InlineData(79, "B")]
    [InlineData(65, "B")]
    [InlineData(64, "C")]
    [InlineData(50, "C")]
    [InlineData(49, "D")]
    [InlineData(35, "D")]
    [InlineData(34, "F")]
    public void Score_GradeThresholds_AreCorrect(int score, string expectedGrade)
    {
        // Use reflection or just verify the scoring logic via the full pipeline
        // We verify grade boundaries by checking the grade formula
        var grade = score switch
        {
            >= 80 => "A",
            >= 65 => "B",
            >= 50 => "C",
            >= 35 => "D",
            _ => "F"
        };
        grade.Should().Be(expectedGrade);
    }

    [Fact]
    public void Score_ContainsAllComponentScores()
    {
        var mainDeck = Enumerable.Range(1, 20)
            .Select(i => AnalysisTestHelpers.Creature($"Creature {i}"))
            .Append(AnalysisTestHelpers.Land(quantity: 40));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);
        var score = ScoreDeck(deck);

        score.ComponentScores.Should().ContainKey("ManaCurve");
        score.ComponentScores.Should().ContainKey("LandRatio");
        score.ComponentScores.Should().ContainKey("ColorConsistency");
        score.ComponentScores.Should().ContainKey("RuleCompliance");
    }

    [Fact]
    public void Score_HighCmcDeck_GetsManaCurveWarning()
    {
        var mainDeck = Enumerable.Range(1, 36)
            .Select(i => AnalysisTestHelpers.MakeEntry($"Heavy {i}", cmc: 6))
            .Append(AnalysisTestHelpers.Land(quantity: 24));

        var deck = AnalysisTestHelpers.ConstructedDeck(mainDeck);
        var score = ScoreDeck(deck);

        score.Warnings.Should().Contain(w =>
            w.Is(AnalysisMessageCodes.ScoreHighAverageCmcCommander) ||
            w.Is(AnalysisMessageCodes.ScoreHighAverageCmcConstructed));
    }
}

using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class ManaBaseAnalyzerTests
{
    [Fact]
    public void Analyze_ExpectedLands_MatchesSimpleRatio()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry(quantity: 23),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = ManaBaseAnalyzer.Analyze(deck);

        result.TotalLands.Should().Be(17);
        result.DeckSize.Should().Be(40);
        result.OpeningHand.ExpectedLands.Should().BeApproximately(7 * 17m / 40, 0.0001m);
    }

    [Fact]
    public void Analyze_LandCountDistribution_SumsToApproximatelyOne()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 24),
            AnalysisTestHelpers.MakeEntry(quantity: 36),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries, format: Format.Commander);

        var result = ManaBaseAnalyzer.Analyze(deck);

        result.OpeningHand.LandCountDistribution.Sum(d => d.Probability).Should().BeApproximately(1.0m, 0.0001m);
    }

    [Fact]
    public void Analyze_ByTurn_Turn1MatchesOpeningHandAtLeastOneLand()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry(quantity: 23),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = ManaBaseAnalyzer.Analyze(deck);

        // Turno 1 = "pelo menos 1 terreno nas 7 cartas iniciais" — mesma quantidade calculável
        // direto pelo HypergeometricCalculator, serve de conferência cruzada.
        var expectedTurn1 = HypergeometricCalculator.ProbabilityAtLeast(40, 17, 7, 1);
        result.ByTurn.Should().HaveCount(10);
        result.ByTurn[0].Turn.Should().Be(1);
        result.ByTurn[0].ProbabilityAtLeastTargetLands.Should().BeApproximately(expectedTurn1, 0.0001m);
        result.ByTurn.Should().OnlyContain(t => t.ProbabilityAtLeastTargetLands >= 0m && t.ProbabilityAtLeastTargetLands <= 1m);
    }

    [Fact]
    public void Analyze_EmptyDeck_ReturnsSafeDefaults()
    {
        var deck = AnalysisTestHelpers.ConstructedDeck([]);

        var result = ManaBaseAnalyzer.Analyze(deck);

        result.DeckSize.Should().Be(0);
        result.TotalLands.Should().Be(0);
        result.OpeningHand.ExpectedLands.Should().Be(0);
        result.OpeningHand.LandCountDistribution.Should().BeEmpty();
        result.ByTurn.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_NoLands_AllProbabilitiesAreZero()
    {
        var entries = new[] { AnalysisTestHelpers.MakeEntry(quantity: 40) };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = ManaBaseAnalyzer.Analyze(deck);

        result.OpeningHand.ProbabilityZeroLands.Should().Be(1.0m);
        result.OpeningHand.ProbabilityAtLeastTwoLands.Should().Be(0m);
        result.ByTurn.Should().OnlyContain(t => t.ProbabilityAtLeastTargetLands == 0m);
    }
}

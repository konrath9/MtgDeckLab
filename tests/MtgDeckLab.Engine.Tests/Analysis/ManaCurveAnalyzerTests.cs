using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class ManaCurveAnalyzerTests
{
    [Fact]
    public void Analyze_SimpleSpells_BuildsCorrectDistribution()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Bolt", cmc: 1, quantity: 4),
            AnalysisTestHelpers.MakeEntry("Counterspell", cmc: 2, quantity: 4),
            AnalysisTestHelpers.MakeEntry("Wrath", cmc: 4, quantity: 2),
        };

        var result = ManaCurveAnalyzer.Analyze(entries);

        result.Distribution[1].Should().Be(4);
        result.Distribution[2].Should().Be(4);
        result.Distribution[4].Should().Be(2);
        result.TotalNonLandCards.Should().Be(10);
    }

    [Fact]
    public void Analyze_LandsAreExcluded()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Bolt", cmc: 1, quantity: 4),
            AnalysisTestHelpers.Land(quantity: 20),
        };

        var result = ManaCurveAnalyzer.Analyze(entries);

        result.TotalNonLandCards.Should().Be(4);
        result.Distribution.Should().NotContainKey(0);
    }

    [Fact]
    public void Analyze_AverageCmc_IsWeightedCorrectly()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry(cmc: 2, quantity: 4), // 4 × 2 = 8
            AnalysisTestHelpers.MakeEntry(cmc: 4, quantity: 4), // 4 × 4 = 16
        };

        var result = ManaCurveAnalyzer.Analyze(entries);

        result.AverageCmc.Should().Be(3.00m); // (8 + 16) / 8
    }

    [Fact]
    public void Analyze_HighCmcBucketsAt7()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Eldrazi", cmc: 10, quantity: 2),
            AnalysisTestHelpers.MakeEntry("Titan", cmc: 7, quantity: 2),
        };

        var result = ManaCurveAnalyzer.Analyze(entries);

        result.Distribution.Should().ContainKey(7);
        result.Distribution[7].Should().Be(4);
        result.Distribution.Should().NotContainKey(10);
    }

    [Fact]
    public void Analyze_PeakCmc_IsHighestBucket()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry(cmc: 1, quantity: 2),
            AnalysisTestHelpers.MakeEntry(cmc: 2, quantity: 8), // peak
            AnalysisTestHelpers.MakeEntry(cmc: 3, quantity: 3),
        };

        var result = ManaCurveAnalyzer.Analyze(entries);

        result.PeakCmc.Should().Be(2);
    }

    [Fact]
    public void Analyze_EmptyEntries_ReturnsZeros()
    {
        var result = ManaCurveAnalyzer.Analyze([]);

        result.TotalNonLandCards.Should().Be(0);
        result.AverageCmc.Should().Be(0);
        result.Distribution.Should().BeEmpty();
    }
}

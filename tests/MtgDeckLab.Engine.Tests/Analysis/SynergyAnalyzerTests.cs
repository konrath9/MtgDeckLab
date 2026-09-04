using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class SynergyAnalyzerTests
{
    [Fact]
    public void Analyze_StrongDominantSignal_IsReportedWithConfidence()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Blood Artist", quantity: 8,
                oracleText: "Whenever a creature you control dies, target player loses 1 life."),
            AnalysisTestHelpers.MakeEntry("Vanilla Bear", quantity: 12),
        };

        var result = SynergyAnalyzer.Analyze(entries);

        result.DominantTag.Should().Be(SynergyTag.Aristocrats);
        result.DominantStrength.Should().Be(0.4m); // 8 / 20
    }

    [Fact]
    public void Analyze_WeakestSignal_NoDominantTagReported()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Blood Artist", quantity: 2,
                oracleText: "Whenever a creature you control dies, target player loses 1 life."),
            AnalysisTestHelpers.MakeEntry("Vanilla Bear", quantity: 18),
        };

        var result = SynergyAnalyzer.Analyze(entries);

        result.DominantTag.Should().BeNull();
        result.LowSynergyWarnings.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_LowSynergyCard_OnlyFlaggedWhenNoRoleAndNoTag()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Blood Artist", quantity: 10,
                oracleText: "Whenever a creature you control dies, target player loses 1 life."),
            AnalysisTestHelpers.MakeEntry("Vanilla Bear", quantity: 8),
            AnalysisTestHelpers.MakeEntry("Doom Blade", quantity: 2, oracleText: "Destroy target creature."),
        };

        var result = SynergyAnalyzer.Analyze(entries);

        // Vanilla Bear não tem role nem synergy tag -> sinalizada.
        result.LowSynergyWarnings.Should().ContainSingle(w => w.Contains("Vanilla Bear"));
        // Doom Blade não bate na tag dominante (Aristocrats), mas tem CardRole.Removal -> não sinalizada.
        result.LowSynergyWarnings.Should().NotContain(w => w.Contains("Doom Blade"));
    }

    [Fact]
    public void Analyze_LandsAreExcludedFromSignalCalculation()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry("Blood Artist", quantity: 10,
                oracleText: "Whenever a creature you control dies, target player loses 1 life."),
            AnalysisTestHelpers.MakeEntry("Vanilla Bear", quantity: 13),
        };

        var result = SynergyAnalyzer.Analyze(entries);

        // 10 / 23 (só não-land), não 10 / 40.
        result.DominantStrength.Should().BeApproximately(10m / 23, 0.0001m);
    }

    [Fact]
    public void Analyze_EmptyDeck_ReturnsEmptyResult()
    {
        var result = SynergyAnalyzer.Analyze([]);

        result.Signals.Should().BeEmpty();
        result.DominantTag.Should().BeNull();
        result.LowSynergyWarnings.Should().BeEmpty();
    }
}

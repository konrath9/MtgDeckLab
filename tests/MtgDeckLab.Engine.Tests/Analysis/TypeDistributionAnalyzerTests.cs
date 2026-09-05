using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class TypeDistributionAnalyzerTests
{
    private static DeckAnalysisEntry BasicLand(string name, int quantity) =>
        new(name, 0, [], [], [CardType.Land], [CardSuperType.Basic], quantity, DeckSection.Main);

    private static DeckAnalysisEntry NonbasicLand(string name, int quantity) =>
        new(name, 0, [], [], [CardType.Land], [], quantity, DeckSection.Main);

    [Fact]
    public void LandBreakdown_SplitsBasicsByColorAndGroupsNonbasics()
    {
        var deck = new[]
        {
            BasicLand("Plains", 4),
            BasicLand("Island", 3),
            NonbasicLand("Command Tower", 2),
            AnalysisTestHelpers.Creature("Bear", quantity: 2),
        };

        var result = TypeDistributionAnalyzer.Analyze(deck);

        result.Lands.Should().Be(9);
        result.LandBreakdown["Plains"].Should().Be(4);
        result.LandBreakdown["Island"].Should().Be(3);
        result.LandBreakdown["Nonbasic"].Should().Be(2);
        result.LandBreakdown.Values.Sum().Should().Be(result.Lands);
    }

    [Fact]
    public void LandBreakdown_SnowBasicsGroupWithTheirColor()
    {
        var result = TypeDistributionAnalyzer.Analyze([BasicLand("Snow-Covered Forest", 5)]);

        result.LandBreakdown["Forest"].Should().Be(5);
    }

    [Fact]
    public void LandBreakdown_ColorlessBasicGetsItsOwnBucket()
    {
        var result = TypeDistributionAnalyzer.Analyze([BasicLand("Wastes", 2)]);

        result.LandBreakdown["Colorless"].Should().Be(2);
    }

    [Fact]
    public void LandBreakdown_IsEmptyWhenDeckHasNoLands()
    {
        var result = TypeDistributionAnalyzer.Analyze([AnalysisTestHelpers.Creature("Bear")]);

        result.Lands.Should().Be(0);
        result.LandBreakdown.Should().BeEmpty();
    }
}

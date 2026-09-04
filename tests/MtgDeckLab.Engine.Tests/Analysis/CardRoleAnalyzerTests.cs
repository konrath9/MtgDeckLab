using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class CardRoleAnalyzerTests
{
    [Fact]
    public void Analyze_CountsCopiesPerRole()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry("Bolt", quantity: 4, oracleText: "Bolt deals 3 damage to any target."),
            AnalysisTestHelpers.MakeEntry("Removal", quantity: 2, oracleText: "Destroy target creature."),
            AnalysisTestHelpers.MakeEntry("Draw", quantity: 3, oracleText: "Draw a card."),
        };

        var result = CardRoleAnalyzer.Analyze(entries);

        result.CardCount[CardRole.Removal].Should().Be(2);
        result.CardCount[CardRole.CardDraw].Should().Be(3);
        result.TotalClassified.Should().Be(5); // Bolt não é classificado em nenhum papel
    }

    [Fact]
    public void Analyze_CardWithMultipleRoles_CountsInEachBucket()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.MakeEntry(
                "Murderous Rider", quantity: 2, oracleText: "Destroy target creature. Draw a card."),
        };

        var result = CardRoleAnalyzer.Analyze(entries);

        result.CardCount[CardRole.Removal].Should().Be(2);
        result.CardCount[CardRole.CardDraw].Should().Be(2);
        result.TotalClassified.Should().Be(2);
    }

    [Fact]
    public void Analyze_NoRolesDetected_ReturnsEmptyDistribution()
    {
        var entries = new[] { AnalysisTestHelpers.MakeEntry("Vanilla Bear", oracleText: null) };

        var result = CardRoleAnalyzer.Analyze(entries);

        result.CardCount.Should().BeEmpty();
        result.TotalClassified.Should().Be(0);
    }
}

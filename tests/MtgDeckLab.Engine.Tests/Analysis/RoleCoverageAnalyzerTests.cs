using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class RoleCoverageAnalyzerTests
{
    [Fact]
    public void Analyze_ConstructedDeck_ClassifiesGreenYellowRedByCount()
    {
        var roles = new RoleDistribution(
            new Dictionary<CardRole, int>
            {
                [CardRole.Removal] = 6, // >= green (6)
                [CardRole.CardDraw] = 2, // >= yellow (2), < green (4)
                [CardRole.Ramp] = 0, // < yellow (2)
            },
            TotalClassified: 8);

        var coverage = RoleCoverageAnalyzer.Analyze(roles, Format.Modern);

        coverage.Entries.Single(e => e.Role == CardRole.Removal).Status.Should().Be(CoverageStatus.Green);
        coverage.Entries.Single(e => e.Role == CardRole.CardDraw).Status.Should().Be(CoverageStatus.Yellow);
        coverage.Entries.Single(e => e.Role == CardRole.Ramp).Status.Should().Be(CoverageStatus.Red);
    }

    [Fact]
    public void Analyze_RedStatus_GeneratesWarning()
    {
        var roles = new RoleDistribution(new Dictionary<CardRole, int>(), TotalClassified: 0);

        var coverage = RoleCoverageAnalyzer.Analyze(roles, Format.Modern);

        coverage.Warnings.Should().NotBeEmpty();
        coverage.Warnings.Should().Contain(w => w.Contains("removal"));
    }

    [Fact]
    public void Analyze_CommanderUsesHigherThresholdsThanConstructed()
    {
        var roles = new RoleDistribution(
            new Dictionary<CardRole, int> { [CardRole.Ramp] = 6 }, TotalClassified: 6);

        var constructedCoverage = RoleCoverageAnalyzer.Analyze(roles, Format.Modern);
        var commanderCoverage = RoleCoverageAnalyzer.Analyze(roles, Format.Commander);

        constructedCoverage.Entries.Single(e => e.Role == CardRole.Ramp).Status.Should().Be(CoverageStatus.Green);
        commanderCoverage.Entries.Single(e => e.Role == CardRole.Ramp).Status.Should().Be(CoverageStatus.Yellow);
    }

    [Fact]
    public void Analyze_CoversAllRolesWithDefinedThresholds()
    {
        var roles = new RoleDistribution(new Dictionary<CardRole, int>(), TotalClassified: 0);

        var coverage = RoleCoverageAnalyzer.Analyze(roles, Format.Modern);

        coverage.Entries.Select(e => e.Role).Should().BeEquivalentTo(
        [
            CardRole.Ramp, CardRole.Removal, CardRole.BoardWipe, CardRole.CardDraw,
            CardRole.Tutor, CardRole.Protection, CardRole.Recursion, CardRole.Interaction
        ]);
    }
}

using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class MonteCarloSimulatorTests
{
    [Fact]
    public void Simulate_NoLands_KeepableHandRateIsZero()
    {
        var entries = new[] { AnalysisTestHelpers.MakeEntry(quantity: 40) };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = MonteCarloSimulator.Simulate(deck, iterations: 500, seed: 1);

        result.KeepableHandRate.Should().Be(0m);
        result.AtLeastTwoLandsHandRate.Should().Be(0m);
    }

    [Fact]
    public void Simulate_AllLands_KeepableHandRateIsZero()
    {
        // 7 terrenos numa mão de 7 cartas está fora da faixa mantível (2-5).
        var entries = new[] { AnalysisTestHelpers.Land(quantity: 40) };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = MonteCarloSimulator.Simulate(deck, iterations: 500, seed: 1);

        result.KeepableHandRate.Should().Be(0m);
        result.AtLeastTwoLandsHandRate.Should().Be(1m);
    }

    [Fact]
    public void Simulate_TypicalDeck_KeepableRateIsWithinPlausibleRange()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry(quantity: 23),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = MonteCarloSimulator.Simulate(deck, iterations: 5000, seed: 42);

        result.KeepableHandRate.Should().BeInRange(0.5m, 0.95m);
    }

    [Fact]
    public void Simulate_RoleAvailability_IsMonotonicAcrossTurns()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry("Filler", quantity: 20),
            AnalysisTestHelpers.MakeEntry("Removal Spell", quantity: 3, oracleText: "Destroy target creature."),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = MonteCarloSimulator.Simulate(deck, iterations: 3000, seed: 7);

        // "Já vi um Removal até o turno T" só pode crescer (ou empatar) conforme T aumenta — ao
        // contrário da curva de land-drop do ManaBaseAnalyzer, aqui não há exigência crescente,
        // só "apareceu pelo menos uma vez entre as cartas vistas", que é estritamente cumulativo.
        var removalByTurn = result.RoleAvailability
            .Where(r => r.Role == CardRole.Removal)
            .OrderBy(r => r.Turn)
            .Select(r => r.Probability)
            .ToList();

        removalByTurn.Should().HaveCount(4);
        for (var i = 1; i < removalByTurn.Count; i++)
            removalByTurn[i].Should().BeGreaterThanOrEqualTo(removalByTurn[i - 1]);
    }

    [Fact]
    public void Simulate_SameSeed_ProducesIdenticalResults()
    {
        var entries = new[]
        {
            AnalysisTestHelpers.Land(quantity: 17),
            AnalysisTestHelpers.MakeEntry(quantity: 23),
        };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var first = MonteCarloSimulator.Simulate(deck, iterations: 1000, seed: 123);
        var second = MonteCarloSimulator.Simulate(deck, iterations: 1000, seed: 123);

        first.KeepableHandRate.Should().Be(second.KeepableHandRate);
        first.AtLeastTwoLandsHandRate.Should().Be(second.AtLeastTwoLandsHandRate);
    }

    [Fact]
    public void Simulate_DeckSmallerThanOpeningHand_ReturnsZeroIterations()
    {
        var entries = new[] { AnalysisTestHelpers.MakeEntry(quantity: 3) };
        var deck = AnalysisTestHelpers.ConstructedDeck(entries);

        var result = MonteCarloSimulator.Simulate(deck, iterations: 100, seed: 1);

        result.Iterations.Should().Be(0);
        result.RoleAvailability.Should().BeEmpty();
    }
}

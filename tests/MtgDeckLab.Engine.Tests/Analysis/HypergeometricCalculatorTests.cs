using FluentAssertions;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class HypergeometricCalculatorTests
{
    [Fact]
    public void ProbabilityExactly_TrivialCoinFlip_IsHalf()
    {
        // População de 2 cartas, 1 sucesso, sorteia 1 carta: 50% de tirar o sucesso.
        var probability = HypergeometricCalculator.ProbabilityExactly(2, 1, 1, 1);

        probability.Should().Be(0.5m);
    }

    [Fact]
    public void ProbabilityExactly_ImpossibleOutcome_IsZero()
    {
        // Não dá pra tirar 5 sucessos numa amostra de 3.
        HypergeometricCalculator.ProbabilityExactly(20, 10, 3, 5).Should().Be(0m);

        // Não dá pra tirar mais sucessos do que existem na população.
        HypergeometricCalculator.ProbabilityExactly(20, 2, 5, 3).Should().Be(0m);
    }

    [Fact]
    public void ProbabilityExactly_SumsAcrossAllK_IsApproximatelyOne()
    {
        const int population = 40, successes = 17, sample = 7;

        var total = Enumerable.Range(0, sample + 1)
            .Sum(k => HypergeometricCalculator.ProbabilityExactly(population, successes, sample, k));

        total.Should().BeApproximately(1.0m, 0.0001m);
    }

    [Fact]
    public void ProbabilityAtLeast_EqualsManualSumOfExactProbabilities()
    {
        const int population = 99, successes = 37, sample = 7;

        var manualSum = Enumerable.Range(2, sample - 1)
            .Sum(k => HypergeometricCalculator.ProbabilityExactly(population, successes, sample, k));

        var atLeast = HypergeometricCalculator.ProbabilityAtLeast(population, successes, sample, 2);

        atLeast.Should().BeApproximately(manualSum, 0.0001m);
    }

    [Fact]
    public void ProbabilityAtLeast_ZeroSuccessesRequired_IsOne()
    {
        HypergeometricCalculator.ProbabilityAtLeast(60, 24, 7, 0).Should().BeApproximately(1.0m, 0.0001m);
    }
}

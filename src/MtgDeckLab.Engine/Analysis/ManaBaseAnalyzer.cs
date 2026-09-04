using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class ManaBaseAnalyzer
{
    private const int OpeningHandSize = 7;
    private const int MaxTurnsToProject = 10;

    public static ManaBaseAnalysis Analyze(DeckForAnalysis deck)
    {
        var mainDeck = deck.MainDeck.ToList();
        var deckSize = mainDeck.Sum(e => e.Quantity);
        var landCount = mainDeck.Where(e => e.IsLand).Sum(e => e.Quantity);

        return new ManaBaseAnalysis(
            landCount, deckSize,
            AnalyzeOpeningHand(deckSize, landCount),
            AnalyzeByTurn(deckSize, landCount));
    }

    private static OpeningHandAnalysis AnalyzeOpeningHand(int deckSize, int landCount)
    {
        if (deckSize < OpeningHandSize)
            return new OpeningHandAnalysis(0m, [], 0m, 0m);

        var distribution = new List<LandProbability>();
        for (var k = 0; k <= Math.Min(landCount, OpeningHandSize); k++)
            distribution.Add(new LandProbability(
                k, HypergeometricCalculator.ProbabilityExactly(deckSize, landCount, OpeningHandSize, k)));

        var expectedLands = OpeningHandSize * (decimal)landCount / deckSize;
        var probabilityZero = HypergeometricCalculator.ProbabilityExactly(deckSize, landCount, OpeningHandSize, 0);
        var probabilityAtLeastTwo = HypergeometricCalculator.ProbabilityAtLeast(deckSize, landCount, OpeningHandSize, 2);

        return new OpeningHandAnalysis(expectedLands, distribution, probabilityZero, probabilityAtLeastTwo);
    }

    private static IReadOnlyList<TurnLandProbability> AnalyzeByTurn(int deckSize, int landCount)
    {
        if (deckSize == 0) return [];

        var results = new List<TurnLandProbability>();
        for (var turn = 1; turn <= MaxTurnsToProject; turn++)
        {
            var cardsSeen = Math.Min(OpeningHandSize + (turn - 1), deckSize);
            results.Add(new TurnLandProbability(
                turn, HypergeometricCalculator.ProbabilityAtLeast(deckSize, landCount, cardsSeen, turn)));
        }

        return results;
    }
}

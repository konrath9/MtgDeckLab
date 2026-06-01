using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class ManaCurveAnalyzer
{
    // CMC >= 7 is bucketed together
    private const int MaxBucket = 7;

    public static ManaCurve Analyze(IEnumerable<DeckAnalysisEntry> mainDeckEntries)
    {
        var distribution = new Dictionary<int, int>();
        decimal totalCmc = 0;
        int totalCards = 0;

        foreach (var entry in mainDeckEntries)
        {
            if (entry.IsLand) continue;

            var bucket = entry.Cmc >= MaxBucket ? MaxBucket : (int)entry.Cmc;
            distribution[bucket] = distribution.GetValueOrDefault(bucket) + entry.Quantity;
            totalCmc += entry.Cmc * entry.Quantity;
            totalCards += entry.Quantity;
        }

        var avgCmc = totalCards > 0 ? Math.Round(totalCmc / totalCards, 2) : 0m;
        var peakCmc = distribution.Count > 0
            ? distribution.MaxBy(kv => kv.Value).Key
            : 0;

        return new ManaCurve(distribution, avgCmc, peakCmc, totalCards);
    }
}

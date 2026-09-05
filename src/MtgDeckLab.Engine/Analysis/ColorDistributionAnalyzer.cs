using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class ColorDistributionAnalyzer
{
    public static ColorDistribution Analyze(IEnumerable<DeckAnalysisEntry> mainDeckEntries)
    {
        var cardCount = new Dictionary<Color, int>();
        int coloredCopies = 0, multicolorCopies = 0;

        foreach (var entry in mainDeckEntries)
        {
            if (entry.Colors.Count == 0) continue;

            foreach (var color in entry.Colors.Distinct())
                cardCount[color] = cardCount.GetValueOrDefault(color) + entry.Quantity;

            coloredCopies += entry.Quantity;
            if (entry.Colors.Distinct().Count() >= 2) multicolorCopies += entry.Quantity;
        }

        var percentage = cardCount.ToDictionary(
            kv => kv.Key,
            kv => coloredCopies > 0 ? Math.Round((double)kv.Value / coloredCopies * 100, 1) : 0.0
        );

        return new ColorDistribution(cardCount, percentage, coloredCopies == 0, multicolorCopies);
    }
}

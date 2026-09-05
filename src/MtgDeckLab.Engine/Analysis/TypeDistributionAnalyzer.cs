using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class TypeDistributionAnalyzer
{
    // Basic land name → bucket. Matched as a substring so snow basics ("Snow-Covered Island")
    // land in the same bucket as their non-snow counterpart.
    private static readonly (string Fragment, string Bucket)[] BasicLandNames =
    [
        ("Plains", "Plains"),
        ("Island", "Island"),
        ("Swamp", "Swamp"),
        ("Mountain", "Mountain"),
        ("Forest", "Forest"),
    ];

    public static TypeDistribution Analyze(IEnumerable<DeckAnalysisEntry> mainDeckEntries)
    {
        int creatures = 0, instants = 0, sorceries = 0, artifacts = 0,
            enchantments = 0, lands = 0, planeswalkers = 0, other = 0, total = 0;
        var landBreakdown = new Dictionary<string, int>();

        foreach (var entry in mainDeckEntries)
        {
            total += entry.Quantity;
            var matched = false;

            if (entry.IsLand)
            {
                var bucket = ClassifyLand(entry);
                landBreakdown[bucket] = landBreakdown.GetValueOrDefault(bucket) + entry.Quantity;
            }

            foreach (var type in entry.Types)
            {
                switch (type)
                {
                    case CardType.Creature: creatures += entry.Quantity; matched = true; break;
                    case CardType.Instant: instants += entry.Quantity; matched = true; break;
                    case CardType.Sorcery: sorceries += entry.Quantity; matched = true; break;
                    case CardType.Artifact: artifacts += entry.Quantity; matched = true; break;
                    case CardType.Enchantment: enchantments += entry.Quantity; matched = true; break;
                    case CardType.Land: lands += entry.Quantity; matched = true; break;
                    case CardType.Planeswalker: planeswalkers += entry.Quantity; matched = true; break;
                }
            }

            if (!matched) other += entry.Quantity;
        }

        return new TypeDistribution(
            creatures, instants, sorceries, artifacts,
            enchantments, lands, planeswalkers, other, total, landBreakdown);
    }

    private static string ClassifyLand(DeckAnalysisEntry entry)
    {
        if (!entry.IsBasicLand) return "Nonbasic";

        foreach (var (fragment, bucket) in BasicLandNames)
            if (entry.CardName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return bucket;

        return "Colorless";
    }
}

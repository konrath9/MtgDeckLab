using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

public static class CardRoleAnalyzer
{
    public static RoleDistribution Analyze(IEnumerable<DeckAnalysisEntry> mainDeckEntries)
    {
        var counts = new Dictionary<CardRole, int>();
        var totalClassified = 0;

        foreach (var entry in mainDeckEntries)
        {
            var roles = entry.Roles;
            if (roles.Count == 0) continue;

            totalClassified += entry.Quantity;
            foreach (var role in roles)
                counts[role] = counts.GetValueOrDefault(role) + entry.Quantity;
        }

        return new RoleDistribution(counts, totalClassified);
    }
}

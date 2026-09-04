using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Engine.Analysis;

// Limiares de "quantas cópias desse papel um deck saudável costuma ter", por formato — guias
// heurísticas, não regra de deckbuilding competitivo. Commander (100 cartas singleton) tende a
// rodar mais utilitários que um deck 60-cartas com 4-ofs, daí os dois conjuntos de limiares.
public static class RoleCoverageAnalyzer
{
    private readonly record struct Thresholds(int Yellow, int Green);

    private static readonly Dictionary<CardRole, Thresholds> CommanderThresholds = new()
    {
        [CardRole.Ramp] = new Thresholds(5, 8),
        [CardRole.CardDraw] = new Thresholds(5, 8),
        [CardRole.Removal] = new Thresholds(5, 8),
        [CardRole.BoardWipe] = new Thresholds(1, 2),
        [CardRole.Protection] = new Thresholds(1, 3),
        [CardRole.Tutor] = new Thresholds(1, 2),
        [CardRole.Recursion] = new Thresholds(1, 2),
        [CardRole.Interaction] = new Thresholds(1, 3),
    };

    private static readonly Dictionary<CardRole, Thresholds> ConstructedThresholds = new()
    {
        [CardRole.Ramp] = new Thresholds(2, 4),
        [CardRole.CardDraw] = new Thresholds(2, 4),
        [CardRole.Removal] = new Thresholds(3, 6),
        [CardRole.BoardWipe] = new Thresholds(1, 2),
        [CardRole.Protection] = new Thresholds(1, 2),
        [CardRole.Tutor] = new Thresholds(1, 2),
        [CardRole.Recursion] = new Thresholds(1, 2),
        [CardRole.Interaction] = new Thresholds(2, 4),
    };

    public static RoleCoverage Analyze(RoleDistribution roles, Format format)
    {
        var thresholds = format == Format.Commander ? CommanderThresholds : ConstructedThresholds;
        var entries = new List<CoverageEntry>();
        var warnings = new List<string>();

        foreach (var role in thresholds.Keys.OrderBy(r => r))
        {
            var threshold = thresholds[role];
            var quantity = roles.CardCount.GetValueOrDefault(role);
            var status = quantity >= threshold.Green ? CoverageStatus.Green
                : quantity >= threshold.Yellow ? CoverageStatus.Yellow
                : CoverageStatus.Red;

            entries.Add(new CoverageEntry(role, quantity, status));

            if (status == CoverageStatus.Red)
                warnings.Add($"Only {quantity} {RoleLabel(role)} card(s) detected. Consider adding more.");
        }

        return new RoleCoverage(entries, warnings);
    }

    private static string RoleLabel(CardRole role) => role switch
    {
        CardRole.CardDraw => "card draw",
        CardRole.BoardWipe => "board wipe",
        _ => role.ToString().ToLowerInvariant()
    };
}

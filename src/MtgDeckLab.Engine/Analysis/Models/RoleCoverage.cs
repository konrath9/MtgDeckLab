using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

public enum CoverageStatus
{
    Red = 0,
    Yellow = 1,
    Green = 2
}

public sealed record CoverageEntry(CardRole Role, int Quantity, CoverageStatus Status);

// Entries cobre um subconjunto fixo de CardRole (os papéis com limiares definidos em
// RoleCoverageAnalyzer) — não é 1:1 com todo o enum CardRole necessariamente.
public sealed record RoleCoverage(IReadOnlyList<CoverageEntry> Entries, IReadOnlyList<string> Warnings);

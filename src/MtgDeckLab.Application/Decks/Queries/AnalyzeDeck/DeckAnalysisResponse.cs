using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;

/// <summary>
/// Contrato da análise exposto pela API: mesma forma do <see cref="DeckAnalysisResult"/> do
/// Engine, com as listas de mensagens já traduzidas para o idioma da requisição. As partes
/// puramente numéricas são reaproveitadas do Engine — não têm texto para traduzir.
/// </summary>
public sealed record DeckAnalysisResponse(
    ManaCurve ManaCurve,
    ColorDistribution ColorDistribution,
    TypeDistribution TypeDistribution,
    RoleDistribution RoleDistribution,
    LocalizedRoleCoverage RoleCoverage,
    ManaBaseAnalysis ManaBase,
    LocalizedSynergyAnalysis Synergy,
    LocalizedValidationResult Validation,
    LocalizedDeckScore Score
);

public sealed record LocalizedValidationResult(
    bool IsValid,
    IReadOnlyList<LocalizedMessage> Errors,
    IReadOnlyList<LocalizedMessage> Warnings
);

public sealed record LocalizedDeckScore(
    int Score,
    string Grade,
    IReadOnlyList<LocalizedMessage> Warnings,
    IReadOnlyDictionary<string, int> ComponentScores
);

public sealed record LocalizedRoleCoverage(
    IReadOnlyList<CoverageEntry> Entries,
    IReadOnlyList<LocalizedMessage> Warnings
);

public sealed record LocalizedSynergyAnalysis(
    IReadOnlyList<SynergySignal> Signals,
    SynergyTag? DominantTag,
    decimal? DominantStrength,
    IReadOnlyList<LocalizedMessage> LowSynergyWarnings
);

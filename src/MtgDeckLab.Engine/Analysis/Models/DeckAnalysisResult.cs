namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record DeckAnalysisResult(
    ManaCurve ManaCurve,
    ColorDistribution ColorDistribution,
    TypeDistribution TypeDistribution,
    RoleDistribution RoleDistribution,
    RoleCoverage RoleCoverage,
    ManaBaseAnalysis ManaBase,
    SynergyAnalysis Synergy,
    AnalysisValidationResult Validation,
    DeckScore Score
);

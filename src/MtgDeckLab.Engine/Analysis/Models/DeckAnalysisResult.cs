namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record DeckAnalysisResult(
    ManaCurve ManaCurve,
    ColorDistribution ColorDistribution,
    TypeDistribution TypeDistribution,
    RoleDistribution RoleDistribution,
    AnalysisValidationResult Validation,
    DeckScore Score
);

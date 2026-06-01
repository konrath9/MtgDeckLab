namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record DeckAnalysisResult(
    ManaCurve ManaCurve,
    ColorDistribution ColorDistribution,
    TypeDistribution TypeDistribution,
    AnalysisValidationResult Validation,
    DeckScore Score
);

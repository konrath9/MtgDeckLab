namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record AnalysisValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

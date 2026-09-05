namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record AnalysisValidationResult(
    bool IsValid,
    IReadOnlyList<AnalysisMessage> Errors,
    IReadOnlyList<AnalysisMessage> Warnings
);

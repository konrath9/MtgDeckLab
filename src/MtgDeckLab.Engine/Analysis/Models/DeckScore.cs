namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record DeckScore(
    int Score,
    string Grade,
    IReadOnlyList<AnalysisMessage> Warnings,
    IReadOnlyDictionary<string, int> ComponentScores
);

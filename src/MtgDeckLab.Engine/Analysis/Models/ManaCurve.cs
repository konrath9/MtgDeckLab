namespace MtgDeckLab.Engine.Analysis.Models;

/// <param name="Distribution">CMC bucket → number of card copies. Bucket 7 = "7+".</param>
/// <param name="AverageCmc">Weighted average CMC of non-land cards.</param>
/// <param name="PeakCmc">CMC bucket with the highest card count.</param>
/// <param name="TotalNonLandCards">Total number of non-land card copies.</param>
public sealed record ManaCurve(
    IReadOnlyDictionary<int, int> Distribution,
    decimal AverageCmc,
    int PeakCmc,
    int TotalNonLandCards
);

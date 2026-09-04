namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record LandProbability(int Lands, decimal Probability);

public sealed record OpeningHandAnalysis(
    decimal ExpectedLands,
    IReadOnlyList<LandProbability> LandCountDistribution,
    decimal ProbabilityZeroLands,
    decimal ProbabilityAtLeastTwoLands
);

public sealed record TurnLandProbability(int Turn, decimal ProbabilityAtLeastTargetLands);

// Hipergeométrica pura: sem mulligan, sem diferenciar jogar primeiro/segundo, sem considerar cor
// dos terrenos — só "quantos terrenos na mão/vistos até o turno T". DeckSize é o MainDeck (exclui
// sideboard e o slot de commander, que não entram na library de onde se compra).
public sealed record ManaBaseAnalysis(
    int TotalLands,
    int DeckSize,
    OpeningHandAnalysis OpeningHand,
    IReadOnlyList<TurnLandProbability> ByTurn
);

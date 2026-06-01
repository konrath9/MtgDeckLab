using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

/// <param name="CardCount">Color → number of card copies with that color pip.</param>
/// <param name="Percentage">Color → % share among all colored card copies.</param>
/// <param name="IsColorless">True when no colored cards exist in the main deck.</param>
public sealed record ColorDistribution(
    IReadOnlyDictionary<Color, int> CardCount,
    IReadOnlyDictionary<Color, double> Percentage,
    bool IsColorless
);

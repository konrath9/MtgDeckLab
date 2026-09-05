using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

/// <param name="CardCount">Color → number of card copies with that color pip.</param>
/// <param name="Percentage">Color → % share among all colored card copies.</param>
/// <param name="IsColorless">True when no colored cards exist in the main deck.</param>
/// <param name="MulticolorCount">Card copies with two or more colors. Counted separately
/// because CardCount tallies each color a card contains, so a gold card appears in several
/// buckets and its multicolor-ness isn't visible from those numbers alone.</param>
public sealed record ColorDistribution(
    IReadOnlyDictionary<Color, int> CardCount,
    IReadOnlyDictionary<Color, double> Percentage,
    bool IsColorless,
    int MulticolorCount
);

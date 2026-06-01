namespace MtgDeckLab.Engine.Analysis.Models;

// Multi-type cards (Artifact Creature) are counted in each applicable bucket.
// Total reflects the actual card copy count, not the sum of buckets.
public sealed record TypeDistribution(
    int Creatures,
    int Instants,
    int Sorceries,
    int Artifacts,
    int Enchantments,
    int Lands,
    int Planeswalkers,
    int Other,
    int Total
);

namespace MtgDeckLab.Engine.Analysis.Models;

// Multi-type cards (Artifact Creature) are counted in each applicable bucket.
// Total reflects the actual card copy count, not the sum of buckets.
//
// LandBreakdown splits the Lands bucket by basic-land color ("Plains".."Forest"), plus
// "Colorless" for colorless basics (Wastes) and "Nonbasic" for everything else — the copy
// counts always sum back to Lands.
public sealed record TypeDistribution(
    int Creatures,
    int Instants,
    int Sorceries,
    int Artifacts,
    int Enchantments,
    int Lands,
    int Planeswalkers,
    int Other,
    int Total,
    IReadOnlyDictionary<string, int> LandBreakdown
);

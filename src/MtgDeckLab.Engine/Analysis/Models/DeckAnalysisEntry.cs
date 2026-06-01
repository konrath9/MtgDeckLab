using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Engine.Analysis.Models;

public sealed record DeckAnalysisEntry(
    string CardName,
    decimal Cmc,
    IReadOnlyList<Color> Colors,
    IReadOnlyList<Color> ColorIdentity,
    IReadOnlyList<CardType> Types,
    IReadOnlyList<CardSuperType> Supertypes,
    int Quantity,
    bool IsCommander,
    bool IsSideboard
)
{
    public bool IsLand => Types.Contains(CardType.Land);
    public bool IsBasicLand => Supertypes.Contains(CardSuperType.Basic) && IsLand;
    public bool IsCreature => Types.Contains(CardType.Creature);
    public bool IsLegendary => Supertypes.Contains(CardSuperType.Legendary);
}

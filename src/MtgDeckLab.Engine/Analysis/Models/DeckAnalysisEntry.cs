using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Analysis.Models;

// OracleText é opcional e vai por último (com default) pra não quebrar as construções
// posicionais já existentes nos testes — ver CardRoleClassifier pra como Roles é inferido.
public sealed record DeckAnalysisEntry(
    string CardName,
    decimal Cmc,
    IReadOnlyList<Color> Colors,
    IReadOnlyList<Color> ColorIdentity,
    IReadOnlyList<CardType> Types,
    IReadOnlyList<CardSuperType> Supertypes,
    int Quantity,
    DeckSection Section,
    string? OracleText = null
)
{
    public bool IsLand => Types.Contains(CardType.Land);
    public bool IsBasicLand => Supertypes.Contains(CardSuperType.Basic) && IsLand;
    public bool IsCreature => Types.Contains(CardType.Creature);
    public bool IsLegendary => Supertypes.Contains(CardSuperType.Legendary);
    public IReadOnlyList<CardRole> Roles => CardRoleClassifier.Classify(OracleText, Types);
    public IReadOnlyList<SynergyTag> SynergyTags => SynergyTagClassifier.Classify(OracleText);
}

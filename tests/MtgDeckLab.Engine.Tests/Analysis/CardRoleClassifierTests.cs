using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class CardRoleClassifierTests
{
    private static readonly CardType[] Instant = [CardType.Instant];
    private static readonly CardType[] Creature = [CardType.Creature];
    private static readonly CardType[] Sorcery = [CardType.Sorcery];
    private static readonly CardType[] Artifact = [CardType.Artifact];
    private static readonly CardType[] Land = [CardType.Land];

    [Fact]
    public void Classify_DestroyTargetCreature_IsRemoval()
    {
        var roles = CardRoleClassifier.Classify("Destroy target creature.", Instant);
        roles.Should().Contain(CardRole.Removal);
    }

    [Fact]
    public void Classify_DestroyAllCreatures_IsBoardWipeNotRemoval()
    {
        var roles = CardRoleClassifier.Classify("Destroy all creatures.", Sorcery);
        roles.Should().Contain(CardRole.BoardWipe);
        roles.Should().NotContain(CardRole.Removal);
    }

    [Fact]
    public void Classify_SearchLibraryForLand_IsRamp()
    {
        var roles = CardRoleClassifier.Classify(
            "Search your library for a basic land card, put it onto the battlefield tapped.", Sorcery);
        roles.Should().Contain(CardRole.Ramp);
    }

    [Fact]
    public void Classify_ManaAbility_IsRamp()
    {
        var roles = CardRoleClassifier.Classify("{T}: Add {C}.", Artifact);
        roles.Should().Contain(CardRole.Ramp);
    }

    [Fact]
    public void Classify_DrawACard_IsCardDraw()
    {
        var roles = CardRoleClassifier.Classify("Draw a card.", Instant);
        roles.Should().Contain(CardRole.CardDraw);
    }

    [Fact]
    public void Classify_SearchLibraryForCreature_IsTutorNotRamp()
    {
        var roles = CardRoleClassifier.Classify(
            "Search your library for a creature card, reveal it, then put it into your hand.", Sorcery);
        roles.Should().Contain(CardRole.Tutor);
        roles.Should().NotContain(CardRole.Ramp);
    }

    [Fact]
    public void Classify_Hexproof_IsProtection()
    {
        var roles = CardRoleClassifier.Classify("Target creature you control gains hexproof until end of turn.", Instant);
        roles.Should().Contain(CardRole.Protection);
    }

    [Fact]
    public void Classify_ReturnCreatureFromGraveyardToHand_IsRecursion()
    {
        var roles = CardRoleClassifier.Classify(
            "Return target creature card from your graveyard to your hand.", Sorcery);
        roles.Should().Contain(CardRole.Recursion);
    }

    [Fact]
    public void Classify_CounterTargetSpell_IsInteraction()
    {
        var roles = CardRoleClassifier.Classify("Counter target spell.", Instant);
        roles.Should().Contain(CardRole.Interaction);
    }

    [Fact]
    public void Classify_MultipleClauses_ReturnsMultipleRoles()
    {
        var roles = CardRoleClassifier.Classify("Destroy target creature. Draw a card.", Instant);
        roles.Should().Contain(CardRole.Removal);
        roles.Should().Contain(CardRole.CardDraw);
    }

    [Fact]
    public void Classify_VanillaCreature_ReturnsNoRoles()
    {
        var roles = CardRoleClassifier.Classify(null, Creature);
        roles.Should().BeEmpty();
    }

    [Fact]
    public void Classify_Land_AlwaysReturnsNoRoles()
    {
        var roles = CardRoleClassifier.Classify(
            "{T}: Add {C}. {T}: Add {G}. Search your library for a Forest card.", Land);
        roles.Should().BeEmpty();
    }
}

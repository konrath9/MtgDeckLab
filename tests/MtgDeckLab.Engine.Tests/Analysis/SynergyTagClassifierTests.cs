using FluentAssertions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Engine.Tests.Analysis;

public class SynergyTagClassifierTests
{
    [Fact]
    public void Classify_SacrificeAndDiesTrigger_IsAristocrats()
    {
        SynergyTagClassifier.Classify("Sacrifice a creature: Draw a card.")
            .Should().Contain(SynergyTag.Aristocrats);

        SynergyTagClassifier.Classify("Whenever a creature you control dies, you gain 1 life.")
            .Should().Contain(SynergyTag.Aristocrats);
    }

    [Fact]
    public void Classify_CreateToken_IsTokens()
    {
        SynergyTagClassifier.Classify("Create a 1/1 white Soldier creature token.")
            .Should().Contain(SynergyTag.Tokens);
    }

    [Fact]
    public void Classify_GainLife_IsLifegain()
    {
        SynergyTagClassifier.Classify("You gain 3 life.").Should().Contain(SynergyTag.Lifegain);
    }

    [Fact]
    public void Classify_FromYourGraveyard_IsGraveyardRecursion()
    {
        SynergyTagClassifier.Classify("Return target creature card from your graveyard to your hand.")
            .Should().Contain(SynergyTag.GraveyardRecursion);
    }

    [Fact]
    public void Classify_WheneverYouCastInstantOrSorcery_IsSpellsMatter()
    {
        SynergyTagClassifier.Classify("Whenever you cast an instant or sorcery spell, scry 1.")
            .Should().Contain(SynergyTag.SpellsMatter);
    }

    [Fact]
    public void Classify_PlusOneCounter_IsPlusOneCounters()
    {
        SynergyTagClassifier.Classify("Put a +1/+1 counter on target creature.")
            .Should().Contain(SynergyTag.PlusOneCounters);
    }

    [Fact]
    public void Classify_ArtifactsYouControl_IsArtifactsMatter()
    {
        SynergyTagClassifier.Classify("Artifacts you control get +1/+1.")
            .Should().Contain(SynergyTag.ArtifactsMatter);
    }

    [Fact]
    public void Classify_MultipleThemes_ReturnsAllMatchingTags()
    {
        var tags = SynergyTagClassifier.Classify(
            "Sacrifice a creature: Create a 1/1 white Soldier creature token and you gain 1 life.");

        tags.Should().Contain(SynergyTag.Aristocrats);
        tags.Should().Contain(SynergyTag.Tokens);
        tags.Should().Contain(SynergyTag.Lifegain);
    }

    [Fact]
    public void Classify_VanillaText_ReturnsNoTags()
    {
        SynergyTagClassifier.Classify("This creature has no abilities.").Should().BeEmpty();
        SynergyTagClassifier.Classify(null).Should().BeEmpty();
    }
}

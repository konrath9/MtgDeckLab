using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Domain.Localization;

namespace MtgDeckLab.Domain.Tests;

public class CardTests
{
    private static Card CreateCard(string name = "Island", Guid? oracleId = null) =>
        new(
            scryfallId: Guid.NewGuid(),
            oracleId: oracleId ?? Guid.NewGuid(),
            name: name,
            manaCost: null,
            cmc: 0,
            colors: [],
            colorIdentity: [Color.Blue],
            typeLine: "Basic Land — Island",
            supertypes: [CardSuperType.Basic],
            types: [CardType.Land],
            subtypes: ["Island"],
            oracleText: null,
            power: null,
            toughness: null,
            loyalty: null,
            priceUsd: null,
            priceUsdFoil: null,
            setCode: "tst");

    [Fact]
    public void NameIn_Should_FallBackToEnglish_When_NoTranslationExists()
    {
        var card = CreateCard();

        Assert.Equal("Island", card.NameIn(CardLanguage.Portuguese));
        Assert.Equal("Island", card.NameIn("es"));
    }

    [Fact]
    public void SetLocalizedName_Should_AddTranslation()
    {
        var card = CreateCard();

        card.SetLocalizedName(CardLanguage.Portuguese, "Ilha", "Terreno Básico — Ilha");

        Assert.Equal("Ilha", card.NameIn(CardLanguage.Portuguese));
        Assert.Equal("Island", card.NameIn(CardLanguage.English));
        var translation = Assert.Single(card.LocalizedNames);
        Assert.Equal("Terreno Básico — Ilha", translation.PrintedTypeLine);
    }

    [Fact]
    public void SetLocalizedName_Should_NormalizeCulture_ToCardLanguage()
    {
        var card = CreateCard();

        card.SetLocalizedName("pt-BR", "Ilha");

        Assert.Equal(CardLanguage.Portuguese, Assert.Single(card.LocalizedNames).Language);
        Assert.Equal("Ilha", card.NameIn("pt-BR"));
    }

    [Fact]
    public void SetLocalizedName_Should_ReplaceExistingTranslation_ForSameLanguage()
    {
        var card = CreateCard();

        card.SetLocalizedName(CardLanguage.Portuguese, "Ilha errada");
        card.SetLocalizedName(CardLanguage.Portuguese, "Ilha");

        Assert.Single(card.LocalizedNames);
        Assert.Equal("Ilha", card.NameIn(CardLanguage.Portuguese));
    }

    // Inglês é o nome canônico da carta; guardá-lo também como "tradução" criaria duas fontes de
    // verdade pro mesmo dado.
    [Fact]
    public void SetLocalizedName_Should_Ignore_English()
    {
        var card = CreateCard();

        card.SetLocalizedName(CardLanguage.English, "Something else");

        Assert.Empty(card.LocalizedNames);
        Assert.Equal("Island", card.Name);
    }

    [Fact]
    public void SyncOracleId_Should_FillEmptyId_ButNeverOverwriteAnExistingOne()
    {
        var card = CreateCard(oracleId: Guid.Empty);
        var oracleId = Guid.NewGuid();

        card.SyncOracleId(oracleId);
        Assert.Equal(oracleId, card.OracleId);

        card.SyncOracleId(Guid.NewGuid());
        Assert.Equal(oracleId, card.OracleId);
    }
}

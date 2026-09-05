using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Tests;

public class DeckTests
{
    private static Deck CreateDeck() => new("Test Deck", Format.Commander, Guid.NewGuid());

    [Fact]
    public void Constructor_Should_InitializeWithNoEntries()
    {
        var deck = CreateDeck();

        Assert.Empty(deck.Entries);
        Assert.Equal(0, deck.TotalMainDeckCards);
        Assert.Equal(0, deck.TotalSideboardCards);
        Assert.Equal(0, deck.TotalMaybeboardCards);
    }

    [Fact]
    public void AddEntry_Should_CreateNewEntry_When_CardNotAlreadyInSection()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();

        deck.AddEntry(cardId, 4, DeckSection.Main);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(cardId, entry.CardId);
        Assert.Equal(4, entry.Quantity);
        Assert.Equal(DeckSection.Main, entry.Section);
    }

    [Fact]
    public void AddEntry_Should_AccumulateQuantity_When_CardAlreadyInSameSection()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();

        deck.AddEntry(cardId, 2, DeckSection.Main);
        deck.AddEntry(cardId, 3, DeckSection.Main);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(5, entry.Quantity);
    }

    [Fact]
    public void AddEntry_Should_TrackSeparateEntries_When_SameCardInDifferentSections()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();

        deck.AddEntry(cardId, 1, DeckSection.Main);
        deck.AddEntry(cardId, 1, DeckSection.Sideboard);

        Assert.Equal(2, deck.Entries.Count);
        Assert.Equal(1, deck.TotalMainDeckCards);
        Assert.Equal(1, deck.TotalSideboardCards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddEntry_Should_Throw_When_QuantityIsNotPositive(int quantity)
    {
        var deck = CreateDeck();

        Assert.Throws<ArgumentOutOfRangeException>(() => deck.AddEntry(Guid.NewGuid(), quantity));
    }

    [Fact]
    public void SetEntryQuantity_Should_CreateEntry_When_NotPresent()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();

        deck.SetEntryQuantity(cardId, 3, DeckSection.Commander);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(3, entry.Quantity);
        Assert.Equal(DeckSection.Commander, entry.Section);
    }

    [Fact]
    public void SetEntryQuantity_Should_OverwriteQuantity_When_Present()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();
        deck.AddEntry(cardId, 4, DeckSection.Main);

        deck.SetEntryQuantity(cardId, 1, DeckSection.Main);

        Assert.Equal(1, Assert.Single(deck.Entries).Quantity);
    }

    [Fact]
    public void SetEntryQuantity_Should_RemoveEntry_When_QuantityIsZero()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();
        deck.AddEntry(cardId, 2, DeckSection.Main);

        deck.SetEntryQuantity(cardId, 0, DeckSection.Main);

        Assert.Empty(deck.Entries);
    }

    [Fact]
    public void SetEntryQuantity_Should_BeNoOp_When_QuantityIsZeroAndEntryDoesNotExist()
    {
        var deck = CreateDeck();

        deck.SetEntryQuantity(Guid.NewGuid(), 0, DeckSection.Main);

        Assert.Empty(deck.Entries);
    }

    [Fact]
    public void SetEntryQuantity_Should_Throw_When_QuantityIsNegative()
    {
        var deck = CreateDeck();

        Assert.Throws<ArgumentOutOfRangeException>(() => deck.SetEntryQuantity(Guid.NewGuid(), -1));
    }

    [Fact]
    public void RemoveEntry_Should_RemoveMatchingEntry()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();
        deck.AddEntry(cardId, 2, DeckSection.Sideboard);

        deck.RemoveEntry(cardId, DeckSection.Sideboard);

        Assert.Empty(deck.Entries);
    }

    [Fact]
    public void RemoveEntry_Should_BeNoOp_When_CardNotInGivenSection()
    {
        var deck = CreateDeck();
        var cardId = Guid.NewGuid();
        deck.AddEntry(cardId, 2, DeckSection.Main);

        deck.RemoveEntry(cardId, DeckSection.Sideboard);

        Assert.Single(deck.Entries);
    }

    [Fact]
    public void ClearSideboard_Should_OnlyRemoveSideboardEntries()
    {
        var deck = CreateDeck();
        deck.AddEntry(Guid.NewGuid(), 1, DeckSection.Main);
        deck.AddEntry(Guid.NewGuid(), 2, DeckSection.Sideboard);
        deck.AddEntry(Guid.NewGuid(), 1, DeckSection.Commander);

        deck.ClearSideboard();

        Assert.Equal(2, deck.Entries.Count);
        Assert.DoesNotContain(deck.Entries, e => e.Section == DeckSection.Sideboard);
    }

    [Fact]
    public void SectionFilters_Should_PartitionEntriesBySection()
    {
        var deck = CreateDeck();
        var mainCard = Guid.NewGuid();
        var sideCard = Guid.NewGuid();
        var commanderCard = Guid.NewGuid();
        var maybeCard = Guid.NewGuid();
        deck.AddEntry(mainCard, 40, DeckSection.Main);
        deck.AddEntry(sideCard, 5, DeckSection.Sideboard);
        deck.AddEntry(commanderCard, 1, DeckSection.Commander);
        deck.AddEntry(maybeCard, 3, DeckSection.Maybeboard);

        Assert.Equal(mainCard, Assert.Single(deck.MainDeck).CardId);
        Assert.Equal(sideCard, Assert.Single(deck.Sideboard).CardId);
        Assert.Equal(commanderCard, Assert.Single(deck.CommanderSlot).CardId);
        Assert.Equal(maybeCard, Assert.Single(deck.Maybeboard).CardId);
        Assert.Equal(40, deck.TotalMainDeckCards);
        Assert.Equal(5, deck.TotalSideboardCards);
        Assert.Equal(3, deck.TotalMaybeboardCards);
    }

    [Fact]
    public void Rename_Should_UpdateName()
    {
        var deck = CreateDeck();

        deck.Rename("New Name");

        Assert.Equal("New Name", deck.Name);
    }

    [Fact]
    public void UpdateDescription_Should_AllowClearingToNull()
    {
        var deck = CreateDeck();
        deck.UpdateDescription("Something");

        deck.UpdateDescription(null);

        Assert.Null(deck.Description);
    }
}

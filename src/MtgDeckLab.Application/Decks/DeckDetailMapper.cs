using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Decks;

internal static class DeckDetailMapper
{
    public static async Task<DeckDetail> ToDetailAsync(
        Deck deck, ICardRepository cardRepo, CancellationToken ct)
    {
        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await cardRepo.FindByIdsAsync(cardIds, ct);
        var cardById = cards.ToDictionary(c => c.Id);

        var entries = deck.Entries
            .Select(e => new DeckEntryDetail(
                e.CardId,
                cardById.TryGetValue(e.CardId, out var card) ? card.Name : "Unknown card",
                e.Quantity, e.IsCommander, e.IsSideboard))
            .ToList();

        return new DeckDetail(
            deck.Id, deck.Name, deck.Format, deck.Description,
            deck.TotalMainDeckCards, deck.TotalSideboardCards,
            deck.CreatedAt, deck.UpdatedAt, entries);
    }
}

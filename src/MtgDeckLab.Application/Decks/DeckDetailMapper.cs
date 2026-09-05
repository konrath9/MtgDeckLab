using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks;

internal static class DeckDetailMapper
{
    private static readonly IReadOnlyList<CardType> NoTypes = Array.Empty<CardType>();

    public static async Task<DeckDetail> ToDetailAsync(
        Deck deck, ICardRepository cardRepo, string cardLanguage, CancellationToken ct)
    {
        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await cardRepo.FindByIdsAsync(cardIds, ct);
        var cardById = cards.ToDictionary(c => c.Id);

        var entries = deck.Entries
            .Select(e =>
            {
                cardById.TryGetValue(e.CardId, out var card);
                return new DeckEntryDetail(
                    e.CardId, card?.Name ?? "Unknown card", LocalizedName(card, cardLanguage),
                    e.Quantity, e.Section, card?.Types ?? NoTypes,
                    card?.Cmc ?? 0, card?.PriceUsd, card?.ManaCost);
            })
            .ToList();

        return new DeckDetail(
            deck.Id, deck.Name, deck.Format, deck.Description,
            deck.TotalMainDeckCards, deck.TotalSideboardCards, deck.TotalMaybeboardCards,
            deck.CreatedAt, deck.UpdatedAt, entries);
    }

    // Nulo quando a carta não tem tradução nesse idioma: o cliente não precisa comparar strings
    // pra saber se o nome mudou, e a resposta não repete o mesmo texto duas vezes.
    private static string? LocalizedName(Card? card, string cardLanguage)
    {
        if (card is null) return null;

        var localized = card.NameIn(cardLanguage);
        return string.Equals(localized, card.Name, StringComparison.Ordinal) ? null : localized;
    }
}

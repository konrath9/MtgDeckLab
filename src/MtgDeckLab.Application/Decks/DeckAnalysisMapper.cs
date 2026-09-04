using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks;

internal static class DeckAnalysisMapper
{
    public static DeckForAnalysis BuildForAnalysis(Deck deck, IEnumerable<Card> cards)
    {
        var cardById = cards.ToDictionary(c => c.Id);

        var entries = deck.Entries
            .Where(e => cardById.ContainsKey(e.CardId))
            .Select(e => ToAnalysisEntry(e, cardById[e.CardId]));

        return new DeckForAnalysis(deck.Name, deck.Format, entries);
    }

    private static DeckAnalysisEntry ToAnalysisEntry(DeckEntry entry, Card card) =>
        new(card.Name, card.Cmc, card.Colors, card.ColorIdentity,
            card.Types, card.Supertypes, entry.Quantity, entry.IsCommander, entry.IsSideboard);
}

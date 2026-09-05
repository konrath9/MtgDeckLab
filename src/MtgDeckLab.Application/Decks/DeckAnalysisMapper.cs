using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks;

internal static class DeckAnalysisMapper
{
    // Maybeboard cards were never actually part of the deck — exclude them here so they never
    // reach score/curve/validation/versioning, which all funnel through this method.
    public static DeckForAnalysis BuildForAnalysis(Deck deck, IEnumerable<Card> cards) =>
        BuildForAnalysis(
            deck.Name, deck.Format,
            deck.Entries
                .Where(e => e.Section != DeckSection.Maybeboard)
                .Select(e => (e.CardId, e.Quantity, e.Section)),
            cards);

    public static DeckForAnalysis BuildForAnalysis(
        string deckName,
        Format format,
        IEnumerable<(Guid CardId, int Quantity, DeckSection Section)> entries,
        IEnumerable<Card> cards)
    {
        var cardById = cards.ToDictionary(c => c.Id);

        var analysisEntries = entries
            .Where(e => cardById.ContainsKey(e.CardId))
            .Select(e => ToAnalysisEntry(cardById[e.CardId], e.Quantity, e.Section));

        return new DeckForAnalysis(deckName, format, analysisEntries);
    }

    private static DeckAnalysisEntry ToAnalysisEntry(Card card, int quantity, DeckSection section) =>
        new(card.Name, card.Cmc, card.Colors, card.ColorIdentity,
            card.Types, card.Supertypes, quantity, section, card.OracleText);
}

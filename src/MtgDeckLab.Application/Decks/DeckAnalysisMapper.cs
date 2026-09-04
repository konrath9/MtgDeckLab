using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks;

internal static class DeckAnalysisMapper
{
    public static DeckForAnalysis BuildForAnalysis(Deck deck, IEnumerable<Card> cards) =>
        BuildForAnalysis(
            deck.Name, deck.Format,
            deck.Entries.Select(e => (e.CardId, e.Quantity, e.IsCommander, e.IsSideboard)),
            cards);

    public static DeckForAnalysis BuildForAnalysis(
        string deckName,
        Format format,
        IEnumerable<(Guid CardId, int Quantity, bool IsCommander, bool IsSideboard)> entries,
        IEnumerable<Card> cards)
    {
        var cardById = cards.ToDictionary(c => c.Id);

        var analysisEntries = entries
            .Where(e => cardById.ContainsKey(e.CardId))
            .Select(e => ToAnalysisEntry(cardById[e.CardId], e.Quantity, e.IsCommander, e.IsSideboard));

        return new DeckForAnalysis(deckName, format, analysisEntries);
    }

    private static DeckAnalysisEntry ToAnalysisEntry(Card card, int quantity, bool isCommander, bool isSideboard) =>
        new(card.Name, card.Cmc, card.Colors, card.ColorIdentity,
            card.Types, card.Supertypes, quantity, isCommander, isSideboard, card.OracleText);
}

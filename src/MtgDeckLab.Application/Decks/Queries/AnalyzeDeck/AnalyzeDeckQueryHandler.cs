using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;

public class AnalyzeDeckQueryHandler : IRequestHandler<AnalyzeDeckQuery, DeckAnalysisResult?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly DeckAnalyzer _analyzer;

    public AnalyzeDeckQueryHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, DeckAnalyzer analyzer)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _analyzer = analyzer;
    }

    public async Task<DeckAnalysisResult?> Handle(AnalyzeDeckQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var cardById = cards.ToDictionary(c => c.Id);

        var entries = deck.Entries
            .Where(e => cardById.ContainsKey(e.CardId))
            .Select(e => ToAnalysisEntry(e, cardById[e.CardId]));

        return _analyzer.Analyze(new DeckForAnalysis(deck.Name, deck.Format, entries));
    }

    private static DeckAnalysisEntry ToAnalysisEntry(DeckEntry entry, Card card) =>
        new(card.Name, card.Cmc, card.Colors, card.ColorIdentity,
            card.Types, card.Supertypes, entry.Quantity, entry.IsCommander, entry.IsSideboard);
}

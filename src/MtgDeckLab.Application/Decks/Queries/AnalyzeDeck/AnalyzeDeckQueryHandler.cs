using MediatR;
using MtgDeckLab.Application.Decks;
using MtgDeckLab.Application.Interfaces;
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

        return _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(deck, cards));
    }
}

using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Engine.Analysis;

namespace MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;

public class AnalyzeDeckQueryHandler : IRequestHandler<AnalyzeDeckQuery, DeckAnalysisResponse?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly DeckAnalyzer _analyzer;
    private readonly IAnalysisMessageLocalizer _localizer;
    private readonly ILanguageContext _language;

    public AnalyzeDeckQueryHandler(
        IDeckRepository deckRepo,
        ICardRepository cardRepo,
        DeckAnalyzer analyzer,
        IAnalysisMessageLocalizer localizer,
        ILanguageContext language)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _analyzer = analyzer;
        _localizer = localizer;
        _language = language;
    }

    public async Task<DeckAnalysisResponse?> Handle(AnalyzeDeckQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);

        var analysis = _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(deck, cards));

        return DeckAnalysisResponseMapper.ToResponse(
            analysis, _localizer, cards, _language.CardLanguage);
    }
}

using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Application.Localization;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckById;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetail?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly ILanguageContext _language;

    public GetDeckByIdQueryHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, ILanguageContext language)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _language = language;
    }

    public async Task<DeckDetail?> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        return await DeckDetailMapper.ToDetailAsync(
            deck, _cardRepo, _language.CardLanguage, cancellationToken);
    }
}

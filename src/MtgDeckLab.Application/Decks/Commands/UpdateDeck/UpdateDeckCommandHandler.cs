using MediatR;
using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Application.Localization;

namespace MtgDeckLab.Application.Decks.Commands.UpdateDeck;

public class UpdateDeckCommandHandler : IRequestHandler<UpdateDeckCommand, DeckDetail>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly ILanguageContext _language;

    public UpdateDeckCommandHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, ILanguageContext language)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _language = language;
    }

    public async Task<DeckDetail> Handle(UpdateDeckCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId)
            throw new KeyNotFoundException($"Deck {request.DeckId} not found.");

        deck.Rename(request.Name);
        deck.UpdateDescription(request.Description);
        await _deckRepo.SaveChangesAsync(cancellationToken);

        return await DeckDetailMapper.ToDetailAsync(
            deck, _cardRepo, _language.CardLanguage, cancellationToken);
    }
}

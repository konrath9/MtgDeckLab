using MediatR;
using MtgDeckLab.Application.Decks;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckById;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetail?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;

    public GetDeckByIdQueryHandler(IDeckRepository deckRepo, ICardRepository cardRepo)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
    }

    public async Task<DeckDetail?> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        return await DeckDetailMapper.ToDetailAsync(deck, _cardRepo, cancellationToken);
    }
}

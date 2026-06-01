using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckById;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetail?>
{
    private readonly IDeckRepository _deckRepo;

    public GetDeckByIdQueryHandler(IDeckRepository deckRepo) => _deckRepo = deckRepo;

    public async Task<DeckDetail?> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        return new DeckDetail(
            deck.Id, deck.Name, deck.Format, deck.Description,
            deck.TotalMainDeckCards, deck.TotalSideboardCards,
            deck.CreatedAt, deck.UpdatedAt);
    }
}

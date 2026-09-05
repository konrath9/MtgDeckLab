using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Exceptions;

namespace MtgDeckLab.Application.Decks.Commands.UpsertDeckEntry;

public class UpsertDeckEntryCommandHandler : IRequestHandler<UpsertDeckEntryCommand, UpsertDeckEntryResult>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;

    public UpsertDeckEntryCommandHandler(IDeckRepository deckRepo, ICardRepository cardRepo)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
    }

    public async Task<UpsertDeckEntryResult> Handle(
        UpsertDeckEntryCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId)
            throw new KeyNotFoundException($"Deck {request.DeckId} not found.");

        var card = await _cardRepo.FindByNameAsync(request.CardName, cancellationToken);
        if (card is null)
            throw new CardNotFoundException(request.CardName);

        deck.SetEntryQuantity(card.Id, request.Quantity, request.Section);
        await _deckRepo.SaveChangesAsync(cancellationToken);

        return new UpsertDeckEntryResult(
            deck.TotalMainDeckCards, deck.TotalSideboardCards, deck.TotalMaybeboardCards);
    }
}

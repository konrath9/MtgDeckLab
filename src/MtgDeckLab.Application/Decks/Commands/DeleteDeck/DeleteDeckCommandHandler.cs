using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Commands.DeleteDeck;

public class DeleteDeckCommandHandler : IRequestHandler<DeleteDeckCommand, Unit>
{
    private readonly IDeckRepository _deckRepo;
    private readonly IFinanceSnapshotRepository _snapshotRepo;

    public DeleteDeckCommandHandler(IDeckRepository deckRepo, IFinanceSnapshotRepository snapshotRepo)
    {
        _deckRepo = deckRepo;
        _snapshotRepo = snapshotRepo;
    }

    public async Task<Unit> Handle(DeleteDeckCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId)
            throw new KeyNotFoundException($"Deck {request.DeckId} not found.");

        await _snapshotRepo.DeleteByDeckIdAsync(request.DeckId, cancellationToken);
        _deckRepo.Remove(deck);
        await _deckRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Decks.Commands.TakeFinanceSnapshot;

public class TakeFinanceSnapshotCommandHandler
    : IRequestHandler<TakeFinanceSnapshotCommand, TakeFinanceSnapshotResult>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly IFinanceSnapshotRepository _snapshotRepo;

    public TakeFinanceSnapshotCommandHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, IFinanceSnapshotRepository snapshotRepo)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _snapshotRepo = snapshotRepo;
    }

    public async Task<TakeFinanceSnapshotResult> Handle(
        TakeFinanceSnapshotCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId)
            throw new KeyNotFoundException($"Deck {request.DeckId} not found.");

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var cardById = cards.ToDictionary(c => c.Id);

        var totalCost = deck.MainDeck.Sum(e =>
            cardById.TryGetValue(e.CardId, out var card) ? (card.PriceUsd ?? 0m) * e.Quantity : 0m);

        var snapshot = new FinanceSnapshot(request.DeckId, totalCost);
        await _snapshotRepo.AddAsync(snapshot, cancellationToken);
        await _snapshotRepo.SaveChangesAsync(cancellationToken);

        return new TakeFinanceSnapshotResult(snapshot.Id, snapshot.TotalCostUsd, snapshot.CreatedAt);
    }
}

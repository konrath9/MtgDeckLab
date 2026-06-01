using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckFinanceSummary;

public class GetDeckFinanceSummaryQueryHandler
    : IRequestHandler<GetDeckFinanceSummaryQuery, DeckFinanceSummary?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly IFinanceSnapshotRepository _snapshotRepo;

    public GetDeckFinanceSummaryQueryHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, IFinanceSnapshotRepository snapshotRepo)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _snapshotRepo = snapshotRepo;
    }

    public async Task<DeckFinanceSummary?> Handle(
        GetDeckFinanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var cardById = cards.ToDictionary(c => c.Id);

        var costEntries = deck.MainDeck
            .Where(e => cardById.TryGetValue(e.CardId, out var c) && c.PriceUsd.HasValue)
            .Select(e =>
            {
                var card = cardById[e.CardId];
                var unit = card.PriceUsd!.Value;
                return new CardCostEntry(card.Name, unit, e.Quantity, unit * e.Quantity);
            })
            .OrderByDescending(e => e.TotalCostUsd)
            .ToList();

        var snapshots = await _snapshotRepo.GetByDeckIdAsync(request.DeckId, cancellationToken);

        return new DeckFinanceSummary(
            request.DeckId,
            costEntries.Sum(e => e.TotalCostUsd),
            costEntries.Take(10).ToList().AsReadOnly(),
            snapshots.Take(10).Select(s => new FinanceSnapshotSummary(s.TotalCostUsd, s.CreatedAt)).ToList().AsReadOnly());
    }
}

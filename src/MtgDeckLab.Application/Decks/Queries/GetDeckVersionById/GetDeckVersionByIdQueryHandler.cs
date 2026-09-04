using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckVersionById;

public class GetDeckVersionByIdQueryHandler : IRequestHandler<GetDeckVersionByIdQuery, DeckVersionDetail?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly IDeckVersionRepository _versionRepo;
    private readonly ICardRepository _cardRepo;

    public GetDeckVersionByIdQueryHandler(
        IDeckRepository deckRepo, IDeckVersionRepository versionRepo, ICardRepository cardRepo)
    {
        _deckRepo = deckRepo;
        _versionRepo = versionRepo;
        _cardRepo = cardRepo;
    }

    public async Task<DeckVersionDetail?> Handle(GetDeckVersionByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var version = await _versionRepo.GetByIdAsync(request.DeckId, request.VersionId, cancellationToken);
        if (version is null) return null;

        var cardIds = version.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var cardById = cards.ToDictionary(c => c.Id);

        var entries = version.Entries
            .Select(e => new DeckVersionEntryDetail(
                e.CardId,
                cardById.TryGetValue(e.CardId, out var card) ? card.Name : "Unknown card",
                e.Quantity, e.IsCommander, e.IsSideboard))
            .ToList();

        return new DeckVersionDetail(
            version.Id, version.VersionNumber, version.Score, version.Grade,
            version.TotalMainDeckCards, version.TotalSideboardCards, version.CreatedAt, entries);
    }
}

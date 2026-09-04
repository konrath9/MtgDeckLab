using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.ListDeckVersions;

public class ListDeckVersionsQueryHandler : IRequestHandler<ListDeckVersionsQuery, IReadOnlyList<DeckVersionSummary>?>
{
    private readonly IDeckRepository _deckRepo;
    private readonly IDeckVersionRepository _versionRepo;

    public ListDeckVersionsQueryHandler(IDeckRepository deckRepo, IDeckVersionRepository versionRepo)
    {
        _deckRepo = deckRepo;
        _versionRepo = versionRepo;
    }

    public async Task<IReadOnlyList<DeckVersionSummary>?> Handle(
        ListDeckVersionsQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var versions = await _versionRepo.GetByDeckIdAsync(request.DeckId, cancellationToken);

        return versions
            .Select(v => new DeckVersionSummary(
                v.Id, v.VersionNumber, v.Score, v.Grade, v.TotalMainDeckCards, v.TotalSideboardCards, v.CreatedAt))
            .ToList();
    }
}

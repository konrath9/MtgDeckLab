using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Decks.Queries.ListDecks;

public class ListDecksQueryHandler : IRequestHandler<ListDecksQuery, PagedResult<DeckSummary>>
{
    private readonly IDeckRepository _deckRepo;

    public ListDecksQueryHandler(IDeckRepository deckRepo) => _deckRepo = deckRepo;

    public async Task<PagedResult<DeckSummary>> Handle(ListDecksQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (decks, totalCount) = await _deckRepo.GetByUserIdAsync(request.UserId, page, pageSize, cancellationToken);

        var items = decks.Select(deck => new DeckSummary(
            deck.Id, deck.Name, deck.Format, deck.Description,
            deck.TotalMainDeckCards, deck.TotalSideboardCards, deck.TotalMaybeboardCards,
            deck.CreatedAt, deck.UpdatedAt)).ToList();

        return new PagedResult<DeckSummary>(items, page, pageSize, totalCount);
    }
}

using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

public class SearchCardsQueryHandler : IRequestHandler<SearchCardsQuery, PagedResult<CardSummary>>
{
    private readonly ICardRepository _cardRepo;

    public SearchCardsQueryHandler(ICardRepository cardRepo) => _cardRepo = cardRepo;

    public async Task<PagedResult<CardSummary>> Handle(SearchCardsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (cards, totalCount) = await _cardRepo.SearchAsync(
            request.Name, request.Type, request.MinCmc, request.MaxCmc, request.SetCode,
            page, pageSize, cancellationToken);

        var items = cards
            .Select(c => new CardSummary(c.Id, c.Name, c.ManaCost, c.Cmc, c.Colors, c.TypeLine, c.PriceUsd, c.SetCode))
            .ToList();

        return new PagedResult<CardSummary>(items, page, pageSize, totalCount);
    }
}

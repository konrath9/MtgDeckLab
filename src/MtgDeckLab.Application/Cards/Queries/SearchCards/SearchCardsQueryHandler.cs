using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

public class SearchCardsQueryHandler : IRequestHandler<SearchCardsQuery, PagedResult<CardSummary>>
{
    private readonly ICardRepository _cardRepo;

    public SearchCardsQueryHandler(ICardRepository cardRepo) => _cardRepo = cardRepo;

    public async Task<PagedResult<CardSummary>> Handle(SearchCardsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (colors, colorlessOnly) = ParseColors(request.Colors);

        var (cards, totalCount) = await _cardRepo.SearchAsync(
            request.Name, request.Type, request.MinCmc, request.MaxCmc, request.SetCode,
            colors, colorlessOnly, page, pageSize, cancellationToken);

        var items = cards
            .Select(c => new CardSummary(c.Id, c.Name, c.ManaCost, c.Cmc, c.Colors, c.TypeLine, c.PriceUsd, c.SetCode))
            .ToList();

        return new PagedResult<CardSummary>(items, page, pageSize, totalCount);
    }

    private static (IReadOnlyList<Color>? Colors, bool ColorlessOnly) ParseColors(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, false);

        var colorlessOnly = false;
        var colors = new List<Color>();

        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (token.ToUpperInvariant())
            {
                case "W": colors.Add(Color.White); break;
                case "U": colors.Add(Color.Blue); break;
                case "B": colors.Add(Color.Black); break;
                case "R": colors.Add(Color.Red); break;
                case "G": colors.Add(Color.Green); break;
                case "C": colorlessOnly = true; break;
            }
        }

        return (colors.Count > 0 ? colors : null, colorlessOnly);
    }
}

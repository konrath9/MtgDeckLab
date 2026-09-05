using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

public class SearchCardsQueryHandler : IRequestHandler<SearchCardsQuery, PagedResult<CardSummary>>
{
    private readonly ICardRepository _cardRepo;
    private readonly ILanguageContext _language;

    public SearchCardsQueryHandler(ICardRepository cardRepo, ILanguageContext language)
    {
        _cardRepo = cardRepo;
        _language = language;
    }

    public async Task<PagedResult<CardSummary>> Handle(SearchCardsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (colors, colorlessOnly) = ParseColors(request.Colors);

        var (cards, totalCount) = await _cardRepo.SearchAsync(
            request.Name, request.Type, request.MinCmc, request.MaxCmc, request.SetCode,
            colors, colorlessOnly, page, pageSize, cancellationToken);

        var language = _language.CardLanguage;
        var items = cards
            .Select(c => new CardSummary(
                c.Id, c.Name, LocalizedName(c, language), c.ManaCost, c.Cmc, c.Colors, c.TypeLine,
                c.PriceUsd, c.SetCode))
            .ToList();

        return new PagedResult<CardSummary>(items, page, pageSize, totalCount);
    }

    private static string? LocalizedName(Card card, string language)
    {
        var localized = card.NameIn(language);
        return string.Equals(localized, card.Name, StringComparison.Ordinal) ? null : localized;
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

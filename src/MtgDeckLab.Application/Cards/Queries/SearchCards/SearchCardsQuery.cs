using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

// Colors: lista separada por vírgula de letras WUBRG (W,U,B,R,G) + C para incolor (ex.: "W,U" = cartas
// que tenham branco E azul entre suas cores; "C" = cartas sem cor colorida).
public record SearchCardsQuery(
    string? Name = null,
    string? Type = null,
    decimal? MinCmc = null,
    decimal? MaxCmc = null,
    string? SetCode = null,
    string? Colors = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<CardSummary>>;

public record CardSummary(
    Guid Id,
    string Name,
    string? ManaCost,
    decimal Cmc,
    IReadOnlyList<Color> Colors,
    string TypeLine,
    decimal? PriceUsd,
    string SetCode
);

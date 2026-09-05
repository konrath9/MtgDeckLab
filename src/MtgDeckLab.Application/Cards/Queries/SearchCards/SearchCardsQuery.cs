using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

// Colors: lista separada por vírgula de letras WUBRG (W,U,B,R,G) + C para incolor (ex.: "W,U" = cartas
// que tenham branco E azul entre suas cores; "C" = cartas sem cor colorida).
// Name casa contra o nome em inglês E contra os nomes traduzidos — buscar "Ilha" acha "Island".
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
    // Nome impresso no idioma do usuário; nulo quando não existe tradução (exibir Name).
    string? LocalizedName,
    string? ManaCost,
    decimal Cmc,
    IReadOnlyList<Color> Colors,
    string TypeLine,
    decimal? PriceUsd,
    string SetCode
);

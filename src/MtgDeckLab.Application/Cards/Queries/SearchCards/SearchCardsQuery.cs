using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Cards.Queries.SearchCards;

public record SearchCardsQuery(
    string? Name = null,
    string? Type = null,
    decimal? MinCmc = null,
    decimal? MaxCmc = null,
    string? SetCode = null,
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

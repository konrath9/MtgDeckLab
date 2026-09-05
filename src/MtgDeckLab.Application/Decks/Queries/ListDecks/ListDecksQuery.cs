using MediatR;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Queries.ListDecks;

public record ListDecksQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<DeckSummary>>;

public record DeckSummary(
    Guid Id,
    string Name,
    Format Format,
    string? Description,
    int MainDeckCount,
    int SideboardCount,
    int MaybeboardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

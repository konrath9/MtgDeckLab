using MediatR;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid DeckId, Guid UserId) : IRequest<DeckDetail?>;

public record DeckDetail(
    Guid Id,
    string Name,
    Format Format,
    string? Description,
    int MainDeckCount,
    int SideboardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

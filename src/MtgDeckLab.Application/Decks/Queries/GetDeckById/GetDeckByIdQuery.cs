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
    int MaybeboardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DeckEntryDetail> Entries
);

public record DeckEntryDetail(
    Guid CardId,
    string CardName,
    int Quantity,
    DeckSection Section,
    IReadOnlyList<CardType> Types,
    decimal Cmc,
    decimal? PriceUsd,
    string? ManaCost
);

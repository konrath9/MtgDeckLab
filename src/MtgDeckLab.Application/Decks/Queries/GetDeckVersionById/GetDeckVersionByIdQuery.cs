using MediatR;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckVersionById;

public record GetDeckVersionByIdQuery(Guid DeckId, Guid VersionId, Guid UserId) : IRequest<DeckVersionDetail?>;

public record DeckVersionDetail(
    Guid Id,
    int VersionNumber,
    int Score,
    string Grade,
    int TotalMainDeckCards,
    int TotalSideboardCards,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DeckVersionEntryDetail> Entries
);

public record DeckVersionEntryDetail(Guid CardId, string CardName, int Quantity, bool IsCommander, bool IsSideboard);

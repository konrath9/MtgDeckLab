using MediatR;

namespace MtgDeckLab.Application.Decks.Queries.ListDeckVersions;

public record ListDeckVersionsQuery(Guid DeckId, Guid UserId) : IRequest<IReadOnlyList<DeckVersionSummary>?>;

public record DeckVersionSummary(
    Guid Id,
    int VersionNumber,
    int Score,
    string Grade,
    int TotalMainDeckCards,
    int TotalSideboardCards,
    DateTimeOffset CreatedAt
);

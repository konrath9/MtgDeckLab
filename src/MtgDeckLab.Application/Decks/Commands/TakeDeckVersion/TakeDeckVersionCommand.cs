using MediatR;

namespace MtgDeckLab.Application.Decks.Commands.TakeDeckVersion;

public record TakeDeckVersionCommand(Guid DeckId, Guid UserId) : IRequest<TakeDeckVersionResult>;

public record TakeDeckVersionResult(
    Guid VersionId,
    int VersionNumber,
    int Score,
    string Grade,
    int TotalMainDeckCards,
    int TotalSideboardCards,
    DateTimeOffset CreatedAt
);

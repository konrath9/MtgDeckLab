using MediatR;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Commands.ImportDeck;

public record ImportDeckCommand(
    string DeckName,
    Format Format,
    string RawDecklist,
    Guid UserId,
    string? Description = null
) : IRequest<ImportDeckResult>;

public record ImportDeckResult(
    Guid DeckId,
    int ResolvedCards,
    IReadOnlyList<string> UnresolvedCardNames
);

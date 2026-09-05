using MediatR;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Decks.Commands.ImportDeck;

public record ImportDeckCommand(
    string DeckName,
    Format Format,
    string MainDecklist,
    Guid UserId,
    string? CommanderDecklist = null,
    string? SideboardDecklist = null,
    string? MaybeboardDecklist = null,
    string? Description = null
) : IRequest<ImportDeckResult>;

public record ImportDeckResult(
    Guid DeckId,
    int ResolvedCards,
    IReadOnlyList<UnresolvedCardName> UnresolvedCardNames
);

public record UnresolvedCardName(string CardName, DeckSection Section);

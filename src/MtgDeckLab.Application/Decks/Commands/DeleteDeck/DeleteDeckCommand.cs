using MediatR;

namespace MtgDeckLab.Application.Decks.Commands.DeleteDeck;

public record DeleteDeckCommand(Guid DeckId, Guid UserId) : IRequest<Unit>;

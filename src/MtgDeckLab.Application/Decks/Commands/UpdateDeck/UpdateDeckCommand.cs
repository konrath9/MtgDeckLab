using MediatR;
using MtgDeckLab.Application.Decks.Queries.GetDeckById;

namespace MtgDeckLab.Application.Decks.Commands.UpdateDeck;

public record UpdateDeckCommand(Guid DeckId, Guid UserId, string Name, string? Description) : IRequest<DeckDetail>;

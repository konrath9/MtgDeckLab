using MediatR;

namespace MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;

public record AnalyzeDeckQuery(Guid DeckId, Guid UserId) : IRequest<DeckAnalysisResponse?>;

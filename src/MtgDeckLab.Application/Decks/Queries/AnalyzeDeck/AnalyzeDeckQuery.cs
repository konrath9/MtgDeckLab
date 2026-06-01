using MediatR;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;

public record AnalyzeDeckQuery(Guid DeckId, Guid UserId) : IRequest<DeckAnalysisResult?>;

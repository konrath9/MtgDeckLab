using MediatR;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckSimulation;

public record GetDeckSimulationQuery(Guid DeckId, Guid UserId, int Iterations = 10_000)
    : IRequest<MonteCarloSimulationResult?>;

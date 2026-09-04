using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckSimulation;

public class GetDeckSimulationQueryHandler : IRequestHandler<GetDeckSimulationQuery, MonteCarloSimulationResult?>
{
    private const int MinIterations = 100;
    private const int MaxIterations = 50_000;

    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;

    public GetDeckSimulationQueryHandler(IDeckRepository deckRepo, ICardRepository cardRepo)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
    }

    public async Task<MonteCarloSimulationResult?> Handle(
        GetDeckSimulationQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct();
        var cards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var deckForAnalysis = DeckAnalysisMapper.BuildForAnalysis(deck, cards);

        var iterations = Math.Clamp(request.Iterations, MinIterations, MaxIterations);
        return MonteCarloSimulator.Simulate(deckForAnalysis, iterations);
    }
}

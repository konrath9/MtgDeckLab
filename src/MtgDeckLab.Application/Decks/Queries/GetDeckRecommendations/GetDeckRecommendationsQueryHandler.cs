using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckRecommendations;

// Recomendação sem IA: pra cada papel que a matriz de cobertura marcou como Red, busca
// candidatos no próprio banco de cartas (cuja color identity é subconjunto da do deck), classifica
// o papel de cada candidato via CardRoleClassifier (mesma heurística usada na análise) e ranqueia
// por RoleFit (papéis extras somam pontos) + proximidade da CMC média do deck.
//
// Não verifica legalidade de formato além de color identity (sem lista de banidas/restritas —
// fora de escopo por falta de fonte de dados) nem limite de cópias (singleton/max 4).
public class GetDeckRecommendationsQueryHandler
    : IRequestHandler<GetDeckRecommendationsQuery, DeckRecommendations?>
{
    private const int MaxCandidatesPerRole = 5;

    private readonly IDeckRepository _deckRepo;
    private readonly ICardRepository _cardRepo;
    private readonly DeckAnalyzer _analyzer;

    public GetDeckRecommendationsQueryHandler(
        IDeckRepository deckRepo, ICardRepository cardRepo, DeckAnalyzer analyzer)
    {
        _deckRepo = deckRepo;
        _cardRepo = cardRepo;
        _analyzer = analyzer;
    }

    public async Task<DeckRecommendations?> Handle(
        GetDeckRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepo.GetByIdAsync(request.DeckId, cancellationToken);
        if (deck is null || deck.UserId != request.UserId) return null;

        var cardIds = deck.Entries.Select(e => e.CardId).Distinct().ToList();
        var deckCards = await _cardRepo.FindByIdsAsync(cardIds, cancellationToken);
        var analysis = _analyzer.Analyze(DeckAnalysisMapper.BuildForAnalysis(deck, deckCards));

        var gaps = analysis.RoleCoverage.Entries.Where(e => e.Status == CoverageStatus.Red).ToList();
        if (gaps.Count == 0)
            return new DeckRecommendations(deck.Id, []);

        var allowedColorIdentity = deckCards.SelectMany(c => c.ColorIdentity).Distinct().ToList();
        var candidates = await _cardRepo.FindRecommendationCandidatesAsync(
            allowedColorIdentity, cardIds, cancellationToken);

        var averageCmc = analysis.ManaCurve.AverageCmc;
        var recommendations = gaps
            .Select(gap => BuildRoleRecommendation(gap, candidates, averageCmc))
            .ToList();

        return new DeckRecommendations(deck.Id, recommendations);
    }

    private static RoleRecommendation BuildRoleRecommendation(
        CoverageEntry gap, IReadOnlyList<Card> candidates, decimal deckAverageCmc)
    {
        var scored = candidates
            .Select(card => (Card: card, Roles: CardRoleClassifier.Classify(card.OracleText, card.Types)))
            .Where(c => c.Roles.Contains(gap.Role))
            .Select(c => new CardRecommendation(
                c.Card.Id, c.Card.Name, c.Card.Cmc, c.Card.ColorIdentity, c.Card.PriceUsd,
                Score(c.Roles.Count, c.Card.Cmc, deckAverageCmc), c.Roles))
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.PriceUsd ?? decimal.MaxValue)
            .Take(MaxCandidatesPerRole)
            .ToList();

        return new RoleRecommendation(gap.Role, gap.Quantity, scored);
    }

    private static int Score(int matchedRoleCount, decimal candidateCmc, decimal deckAverageCmc)
    {
        var versatilityBonus = (matchedRoleCount - 1) * 10;
        var curveFitPenalty = (int)(Math.Abs(candidateCmc - deckAverageCmc) * 5);
        return Math.Max(100 + versatilityBonus - curveFitPenalty, 0);
    }
}

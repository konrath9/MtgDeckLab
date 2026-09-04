using MediatR;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckRecommendations;

public record GetDeckRecommendationsQuery(Guid DeckId, Guid UserId) : IRequest<DeckRecommendations?>;

// Uma entrada por CardRole que a matriz de cobertura marcou como Red — ver RoleCoverageAnalyzer.
public record DeckRecommendations(Guid DeckId, IReadOnlyList<RoleRecommendation> Recommendations);

public record RoleRecommendation(
    CardRole Role, int CurrentQuantity, IReadOnlyList<CardRecommendation> Candidates);

public record CardRecommendation(
    Guid CardId,
    string CardName,
    decimal Cmc,
    IReadOnlyList<Color> ColorIdentity,
    decimal? PriceUsd,
    int Score,
    IReadOnlyList<CardRole> MatchedRoles
);

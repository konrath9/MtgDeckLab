using MediatR;

namespace MtgDeckLab.Application.Decks.Queries.GetDeckFinanceSummary;

public record GetDeckFinanceSummaryQuery(Guid DeckId, Guid UserId) : IRequest<DeckFinanceSummary?>;

public record DeckFinanceSummary(
    Guid DeckId,
    decimal TotalCostUsd,
    IReadOnlyList<CardCostEntry> TopExpensiveCards,
    IReadOnlyList<FinanceSnapshotSummary> RecentSnapshots
);

public record CardCostEntry(string CardName, decimal UnitPriceUsd, int Quantity, decimal TotalCostUsd);

public record FinanceSnapshotSummary(decimal TotalCostUsd, DateTimeOffset CreatedAt);

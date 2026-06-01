using MediatR;

namespace MtgDeckLab.Application.Decks.Commands.TakeFinanceSnapshot;

public record TakeFinanceSnapshotCommand(Guid DeckId, Guid UserId) : IRequest<TakeFinanceSnapshotResult>;

public record TakeFinanceSnapshotResult(Guid SnapshotId, decimal TotalCostUsd, DateTimeOffset CreatedAt);

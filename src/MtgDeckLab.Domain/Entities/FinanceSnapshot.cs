namespace MtgDeckLab.Domain.Entities;

public sealed class FinanceSnapshot
{
    public Guid Id { get; private init; }
    public Guid DeckId { get; private init; }
    public decimal TotalCostUsd { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }

    private FinanceSnapshot() { }

    public FinanceSnapshot(Guid deckId, decimal totalCostUsd)
    {
        Id = Guid.NewGuid();
        DeckId = deckId;
        TotalCostUsd = totalCostUsd;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Interfaces;

public interface IFinanceSnapshotRepository
{
    Task AddAsync(FinanceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<FinanceSnapshot>> GetByDeckIdAsync(Guid deckId, CancellationToken ct = default);
    Task DeleteByDeckIdAsync(Guid deckId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

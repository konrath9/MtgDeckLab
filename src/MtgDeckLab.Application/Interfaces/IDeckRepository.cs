using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Interfaces;

public interface IDeckRepository
{
    Task<Deck?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Deck> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Deck deck, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

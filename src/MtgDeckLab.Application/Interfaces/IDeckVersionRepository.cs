using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Interfaces;

public interface IDeckVersionRepository
{
    Task AddAsync(DeckVersion version, CancellationToken ct = default);
    Task<IReadOnlyList<DeckVersion>> GetByDeckIdAsync(Guid deckId, CancellationToken ct = default);
    Task<DeckVersion?> GetByIdAsync(Guid deckId, Guid versionId, CancellationToken ct = default);
    Task<int> GetNextVersionNumberAsync(Guid deckId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

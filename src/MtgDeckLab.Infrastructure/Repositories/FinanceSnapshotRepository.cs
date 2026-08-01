using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.Infrastructure.Repositories;

public class FinanceSnapshotRepository : IFinanceSnapshotRepository
{
    private readonly MtgDeckLabDbContext _context;

    public FinanceSnapshotRepository(MtgDeckLabDbContext context) => _context = context;

    public async Task AddAsync(FinanceSnapshot snapshot, CancellationToken ct = default) =>
        await _context.FinanceSnapshots.AddAsync(snapshot, ct);

    public async Task<IReadOnlyList<FinanceSnapshot>> GetByDeckIdAsync(Guid deckId, CancellationToken ct = default) =>
        await _context.FinanceSnapshots
            .Where(s => s.DeckId == deckId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task DeleteByDeckIdAsync(Guid deckId, CancellationToken ct = default) =>
        await _context.FinanceSnapshots
            .Where(s => s.DeckId == deckId)
            .ExecuteDeleteAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}

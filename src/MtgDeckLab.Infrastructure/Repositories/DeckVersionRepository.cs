using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.Infrastructure.Repositories;

public class DeckVersionRepository : IDeckVersionRepository
{
    private readonly MtgDeckLabDbContext _context;

    public DeckVersionRepository(MtgDeckLabDbContext context) => _context = context;

    public async Task AddAsync(DeckVersion version, CancellationToken ct = default) =>
        await _context.DeckVersions.AddAsync(version, ct);

    public async Task<IReadOnlyList<DeckVersion>> GetByDeckIdAsync(Guid deckId, CancellationToken ct = default) =>
        await _context.DeckVersions
            .Include(v => v.Entries)
            .Where(v => v.DeckId == deckId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

    public async Task<DeckVersion?> GetByIdAsync(Guid deckId, Guid versionId, CancellationToken ct = default) =>
        await _context.DeckVersions
            .Include(v => v.Entries)
            .FirstOrDefaultAsync(v => v.DeckId == deckId && v.Id == versionId, ct);

    public async Task<int> GetNextVersionNumberAsync(Guid deckId, CancellationToken ct = default)
    {
        var maxVersion = await _context.DeckVersions
            .Where(v => v.DeckId == deckId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(ct);

        return (maxVersion ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}

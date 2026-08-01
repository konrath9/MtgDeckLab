using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.Infrastructure.Repositories;

public class DeckRepository : IDeckRepository
{
    private readonly MtgDeckLabDbContext _context;

    public DeckRepository(MtgDeckLabDbContext context) => _context = context;

    public async Task<Deck?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Decks
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<Deck> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Decks.Where(d => d.UserId == userId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Include(d => d.Entries)
            .AsSplitQuery()
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Deck deck, CancellationToken ct = default) =>
        await _context.Decks.AddAsync(deck, ct);

    public void Remove(Deck deck) => _context.Decks.Remove(deck);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}

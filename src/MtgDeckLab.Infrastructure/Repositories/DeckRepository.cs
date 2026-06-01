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

    public async Task AddAsync(Deck deck, CancellationToken ct = default) =>
        await _context.Decks.AddAsync(deck, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}

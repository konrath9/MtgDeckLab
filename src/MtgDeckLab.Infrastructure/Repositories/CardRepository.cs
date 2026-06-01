using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.Infrastructure.Repositories;

public class CardRepository : ICardRepository
{
    private readonly MtgDeckLabDbContext _context;

    public CardRepository(MtgDeckLabDbContext context) => _context = context;

    public async Task<Card?> FindByNameAsync(string name, CancellationToken ct = default) =>
        await _context.Cards
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower(), ct);

    public async Task<IReadOnlyList<Card>> FindByNamesAsync(IEnumerable<string> names, CancellationToken ct = default)
    {
        var lowerNames = names.Select(n => n.ToLowerInvariant()).ToList();
        return await _context.Cards
            .Where(c => lowerNames.Contains(c.Name.ToLower()))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Card>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _context.Cards.Where(c => idList.Contains(c.Id)).ToListAsync(ct);
    }

    public async Task<Card?> FindByScryfallIdAsync(Guid scryfallId, CancellationToken ct = default) =>
        await _context.Cards.FirstOrDefaultAsync(c => c.ScryfallId == scryfallId, ct);

    public async Task UpsertAsync(Card card, CancellationToken ct = default)
    {
        var exists = await _context.Cards.AnyAsync(c => c.ScryfallId == card.ScryfallId, ct);
        if (!exists)
            _context.Cards.Add(card);

        await _context.SaveChangesAsync(ct);
    }

    public async Task UpsertManyAsync(IEnumerable<Card> cards, CancellationToken ct = default)
    {
        var cardList = cards.ToList();
        if (cardList.Count == 0) return;

        var scryfallIds = cardList.Select(c => c.ScryfallId).ToList();

        var existingCards = await _context.Cards
            .Where(c => scryfallIds.Contains(c.ScryfallId))
            .ToListAsync(ct);

        var existingById = existingCards.ToDictionary(c => c.ScryfallId);

        var toAdd = cardList.Where(c => !existingById.ContainsKey(c.ScryfallId)).ToList();

        // Update prices for cards that already exist
        foreach (var incoming in cardList.Where(c => existingById.ContainsKey(c.ScryfallId)))
            existingById[incoming.ScryfallId].UpdatePrices(incoming.PriceUsd, incoming.PriceUsdFoil);

        if (toAdd.Count > 0)
            await _context.Cards.AddRangeAsync(toAdd, ct);

        await _context.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;
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

    // Types/Supertypes/Subtypes ainda são persistidos como JSON via ValueConverter (ver
    // CardConfiguration), então não são traduzíveis pra SQL. Colors/ColorIdentity são integer[]
    // nativo do Postgres — acessados via EF.Property(shadow field) pra permitir filtro por cor.
    public async Task<(IReadOnlyList<Card> Items, int TotalCount)> SearchAsync(
        string? name, string? type, decimal? minCmc, decimal? maxCmc, string? setCode,
        IReadOnlyList<Color>? colors, bool colorlessOnly,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Cards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name.ToLower().Contains(name.ToLower()));

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(c => c.TypeLine.ToLower().Contains(type.ToLower()));

        if (minCmc.HasValue)
            query = query.Where(c => c.Cmc >= minCmc.Value);

        if (maxCmc.HasValue)
            query = query.Where(c => c.Cmc <= maxCmc.Value);

        if (!string.IsNullOrWhiteSpace(setCode))
            query = query.Where(c => c.SetCode.ToLower() == setCode.ToLower());

        if (colors is { Count: > 0 })
            foreach (var color in colors)
                query = query.Where(c => EF.Property<List<Color>>(c, "_colors").Contains(color));

        if (colorlessOnly)
            query = query.Where(c => EF.Property<List<Color>>(c, "_colors").Count == 0);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    // "Subset of" (card's color identity ⊆ allowedColorIdentity) traduz pra `<@` no Postgres via
    // o padrão array.All(x => otherArray.Contains(x)) que o provider Npgsql reconhece.
    public async Task<IReadOnlyList<Card>> FindRecommendationCandidatesAsync(
        IReadOnlyList<Color> allowedColorIdentity, IReadOnlyCollection<Guid> excludeCardIds,
        CancellationToken ct = default) =>
        await _context.Cards
            .Where(c => !excludeCardIds.Contains(c.Id))
            .Where(c => !c.TypeLine.ToLower().Contains("land"))
            .Where(c => EF.Property<List<Color>>(c, "_colorIdentity").All(ci => allowedColorIdentity.Contains(ci)))
            .ToListAsync(ct);

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

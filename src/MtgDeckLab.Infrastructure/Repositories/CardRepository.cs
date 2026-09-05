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

    // Toda leitura que devolve cartas pra exibição traz junto os nomes traduzidos: é o que permite
    // à camada de cima resolver Card.NameIn(idioma) sem uma segunda ida ao banco por carta.
    private IQueryable<Card> CardsWithNames => _context.Cards.Include(c => c.LocalizedNames);

    public async Task<Card?> FindByNameAsync(string name, CancellationToken ct = default)
    {
        var lower = name.ToLowerInvariant();

        var exact = await CardsWithNames.FirstOrDefaultAsync(
            c => c.Name.ToLower() == lower || c.LocalizedNames.Any(n => n.Name.ToLower() == lower), ct);
        if (exact is not null) return exact;

        // Modal double-faced / split cards are stored as "Front // Back" (their full Scryfall
        // name) — decklists and manual entry only ever reference the front face.
        var prefix = lower + " // ";
        return await CardsWithNames.FirstOrDefaultAsync(
            c => c.Name.ToLower().StartsWith(prefix) ||
                 c.LocalizedNames.Any(n => n.Name.ToLower().StartsWith(prefix)), ct);
    }

    public async Task<IReadOnlyList<Card>> FindByNamesAsync(IEnumerable<string> names, CancellationToken ct = default)
    {
        var lowerNames = names.Select(n => n.ToLowerInvariant()).ToHashSet();

        var exactMatches = await CardsWithNames
            .Where(c => lowerNames.Contains(c.Name.ToLower()) ||
                        c.LocalizedNames.Any(n => lowerNames.Contains(n.Name.ToLower())))
            .ToListAsync(ct);

        var matchedNames = exactMatches
            .SelectMany(c => c.LocalizedNames.Select(n => n.Name).Append(c.Name))
            .Select(n => n.ToLowerInvariant())
            .ToHashSet();

        var stillMissing = lowerNames.Where(n => !matchedNames.Contains(n)).ToHashSet();
        if (stillMissing.Count == 0) return exactMatches;

        // Same front-face fallback as FindByNameAsync, batched: pull the (small) set of
        // double-faced/split cards once and match front names in memory.
        var dfcCandidates = await CardsWithNames
            .Where(c => c.Name.Contains(" // ") || c.LocalizedNames.Any(n => n.Name.Contains(" // ")))
            .ToListAsync(ct);

        var frontFaceMatches = dfcCandidates
            .Where(c => FrontFaces(c).Any(stillMissing.Contains));

        return exactMatches.Concat(frontFaceMatches).ToList();
    }

    public async Task<IReadOnlyList<Card>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await CardsWithNames.Where(c => idList.Contains(c.Id)).ToListAsync(ct);
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
        var query = CardsWithNames;

        // Nome casa contra o inglês OU qualquer tradução: quem digita "Ilha" acha "Island" sem
        // precisar saber em que idioma a carta foi impressa.
        if (!string.IsNullOrWhiteSpace(name))
        {
            var lower = name.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(lower) ||
                c.LocalizedNames.Any(n => n.Name.ToLower().Contains(lower)));
        }

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

    // "Subset of" (color identity da carta ⊆ allowedColorIdentity) traduz pra `<@` no Postgres via
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

        foreach (var incoming in cardList.Where(c => existingById.ContainsKey(c.ScryfallId)))
        {
            var existing = existingById[incoming.ScryfallId];
            existing.UpdatePrices(incoming.PriceUsd, incoming.PriceUsdFoil);
            // Linhas gravadas antes de oracle_id existir ganham o seu no primeiro sync seguinte —
            // sem isso elas nunca casariam com nenhuma tradução.
            existing.SyncOracleId(incoming.OracleId);
        }

        if (toAdd.Count > 0)
            await _context.Cards.AddRangeAsync(toAdd, ct);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch
        {
            // O DbContext é scoped e reaproveitado por todos os lotes de um sync — sem isso, as
            // entidades desse lote falho ficariam "grudadas" no change tracker e envenenariam
            // (fariam falhar) todo lote seguinte também.
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<int> UpsertTranslationsAsync(
        IReadOnlyCollection<CardTranslation> translations, CancellationToken ct = default)
    {
        if (translations.Count == 0) return 0;

        var oracleIds = translations.Select(t => t.OracleId).Distinct().ToList();

        var cards = await _context.Cards
            .Include(c => c.LocalizedNames)
            .Where(c => oracleIds.Contains(c.OracleId))
            .ToListAsync(ct);

        // Uma mesma oracle id pode ter mais de uma linha na tabela de cartas (impressões que o
        // sync manteve) — todas recebem o mesmo nome traduzido.
        var cardsByOracleId = cards
            .GroupBy(c => c.OracleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var applied = 0;
        foreach (var translation in translations)
        {
            if (!cardsByOracleId.TryGetValue(translation.OracleId, out var matches)) continue;

            foreach (var card in matches)
                card.SetLocalizedName(translation.Language, translation.Name, translation.PrintedTypeLine);

            applied++;
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }

        return applied;
    }

    private static IEnumerable<string> FrontFaces(Card card) =>
        card.LocalizedNames
            .Select(n => n.Name)
            .Append(card.Name)
            .Where(n => n.Contains(" // "))
            .Select(n => n.Split(" // ")[0].ToLowerInvariant());
}

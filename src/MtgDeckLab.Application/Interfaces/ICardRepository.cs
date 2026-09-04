using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Interfaces;

public interface ICardRepository
{
    Task<Card?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> FindByNamesAsync(IEnumerable<string> names, CancellationToken ct = default);
    Task<Card?> FindByScryfallIdAsync(Guid scryfallId, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<(IReadOnlyList<Card> Items, int TotalCount)> SearchAsync(
        string? name, string? type, decimal? minCmc, decimal? maxCmc, string? setCode,
        IReadOnlyList<Color>? colors, bool colorlessOnly,
        int page, int pageSize, CancellationToken ct = default);
    Task UpsertAsync(Card card, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<Card> cards, CancellationToken ct = default);
}

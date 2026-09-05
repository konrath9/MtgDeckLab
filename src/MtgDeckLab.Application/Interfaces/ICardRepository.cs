using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Interfaces;

/// <summary>
/// Nome traduzido de uma carta vindo da Scryfall, endereçado pelo oracle id — o mesmo id em
/// qualquer idioma e em qualquer reimpressão.
/// </summary>
public sealed record CardTranslation(
    Guid OracleId,
    string Language,
    string Name,
    string? PrintedTypeLine
);

public interface ICardRepository
{
    /// <summary>
    /// Busca uma carta pelo nome em <em>qualquer</em> idioma sincronizado — o usuário digita
    /// "Ilha" ou "Island" e a mesma carta é encontrada.
    /// </summary>
    Task<Card?> FindByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Versão em lote de <see cref="FindByNameAsync"/> (usada na importação de decklist).</summary>
    Task<IReadOnlyList<Card>> FindByNamesAsync(IEnumerable<string> names, CancellationToken ct = default);

    Task<Card?> FindByScryfallIdAsync(Guid scryfallId, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<(IReadOnlyList<Card> Items, int TotalCount)> SearchAsync(
        string? name, string? type, decimal? minCmc, decimal? maxCmc, string? setCode,
        IReadOnlyList<Color>? colors, bool colorlessOnly,
        int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> FindRecommendationCandidatesAsync(
        IReadOnlyList<Color> allowedColorIdentity, IReadOnlyCollection<Guid> excludeCardIds,
        CancellationToken ct = default);
    Task UpsertAsync(Card card, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<Card> cards, CancellationToken ct = default);

    /// <summary>
    /// Grava nomes traduzidos, casando pelo oracle id. Traduções sem carta correspondente são
    /// ignoradas (a Scryfall publica impressões que o sync de cartas descarta, como tokens).
    /// </summary>
    /// <returns>Quantas traduções foram efetivamente aplicadas.</returns>
    Task<int> UpsertTranslationsAsync(
        IReadOnlyCollection<CardTranslation> translations, CancellationToken ct = default);
}

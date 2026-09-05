using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Interfaces;

public interface IScryfallSyncService
{
    /// <summary>Cartas canônicas (inglês), uma por oracle id.</summary>
    IAsyncEnumerable<Card> StreamOracleCardsAsync(CancellationToken ct = default);

    /// <summary>
    /// Nomes impressos das cartas nos idiomas pedidos, um por oracle id/idioma.
    /// </summary>
    /// <param name="languages">Códigos Scryfall (ex.: <c>["pt"]</c>). Inglês é ignorado — já é o nome canônico.</param>
    IAsyncEnumerable<CardTranslation> StreamCardTranslationsAsync(
        IReadOnlyCollection<string> languages, CancellationToken ct = default);
}

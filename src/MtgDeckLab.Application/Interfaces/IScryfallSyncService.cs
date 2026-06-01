using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Interfaces;

public interface IScryfallSyncService
{
    IAsyncEnumerable<Card> StreamOracleCardsAsync(CancellationToken ct = default);
}

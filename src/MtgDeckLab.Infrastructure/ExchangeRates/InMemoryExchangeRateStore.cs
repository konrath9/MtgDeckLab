using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Infrastructure.ExchangeRates;

/// <summary>
/// Singleton thread-safe — lido por toda requisição que formata preço, escrito uma vez por dia
/// pelo <see cref="ExchangeRateSyncBackgroundService"/> (ou sob demanda via admin). Um lock simples
/// é suficiente: a escrita é rara e a leitura é só a troca de uma referência imutável.
/// </summary>
public sealed class InMemoryExchangeRateStore : IExchangeRateStore
{
    private readonly object _lock = new();
    private CachedExchangeRate? _current;

    public CachedExchangeRate? Current
    {
        get { lock (_lock) return _current; }
    }

    public void Set(decimal usdToBrl, DateTimeOffset fetchedAt)
    {
        lock (_lock) _current = new CachedExchangeRate(usdToBrl, fetchedAt);
    }
}

namespace MtgDeckLab.Application.Interfaces;

public sealed record CachedExchangeRate(decimal UsdToBrl, DateTimeOffset FetchedAt);

/// <summary>
/// Cache em memória da última cotação USD→BRL sincronizada — não é persistido em banco de
/// propósito: é um único escalar, refeito diariamente, e perder o valor num restart só significa
/// esperar o próximo sync (que roda logo na subida, não só depois de um dia — ver
/// <c>ExchangeRateSyncBackgroundService</c>).
/// </summary>
public interface IExchangeRateStore
{
    CachedExchangeRate? Current { get; }

    void Set(decimal usdToBrl, DateTimeOffset fetchedAt);
}

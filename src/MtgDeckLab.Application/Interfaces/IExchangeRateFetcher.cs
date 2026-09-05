namespace MtgDeckLab.Application.Interfaces;

/// <summary>
/// Busca a cotação atual USD→BRL numa fonte externa (implementado na Infrastructure).
/// </summary>
public interface IExchangeRateFetcher
{
    /// <returns>A cotação, ou <c>null</c> se a fonte externa falhar ou não trouxer o dado.</returns>
    Task<decimal?> FetchUsdToBrlAsync(CancellationToken ct = default);
}

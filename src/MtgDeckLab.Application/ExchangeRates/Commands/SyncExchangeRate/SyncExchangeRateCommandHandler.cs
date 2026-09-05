using MediatR;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.ExchangeRates.Commands.SyncExchangeRate;

public class SyncExchangeRateCommandHandler : IRequestHandler<SyncExchangeRateCommand, SyncExchangeRateResult>
{
    private readonly IExchangeRateFetcher _fetcher;
    private readonly IExchangeRateStore _store;
    private readonly ILogger<SyncExchangeRateCommandHandler> _logger;

    public SyncExchangeRateCommandHandler(
        IExchangeRateFetcher fetcher, IExchangeRateStore store, ILogger<SyncExchangeRateCommandHandler> logger)
    {
        _fetcher = fetcher;
        _store = store;
        _logger = logger;
    }

    public async Task<SyncExchangeRateResult> Handle(SyncExchangeRateCommand request, CancellationToken cancellationToken)
    {
        var rate = await _fetcher.FetchUsdToBrlAsync(cancellationToken);

        if (rate is null)
        {
            // Não sobrescreve o cache com "nada" — um valor de ontem ainda é melhor exibição do
            // que nenhum, e o próximo ciclo tenta de novo.
            _logger.LogWarning("Could not fetch USD→BRL exchange rate; keeping previous cached value.");
            return new SyncExchangeRateResult(false, null, null);
        }

        var fetchedAt = DateTimeOffset.UtcNow;
        _store.Set(rate.Value, fetchedAt);

        _logger.LogInformation("USD→BRL exchange rate synced: {Rate} at {FetchedAt}.", rate.Value, fetchedAt);
        return new SyncExchangeRateResult(true, rate.Value, fetchedAt);
    }
}

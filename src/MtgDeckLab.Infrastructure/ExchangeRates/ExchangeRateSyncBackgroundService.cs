using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.ExchangeRates.Commands.SyncExchangeRate;

namespace MtgDeckLab.Infrastructure.ExchangeRates;

/// <summary>
/// Roda o sync da cotação USD→BRL diariamente. Ao contrário do
/// <see cref="Scryfall.ScryfallSyncBackgroundService"/> (que espera um intervalo antes do primeiro
/// disparo — um sync de cartas é pesado, minutos de download), aqui o primeiro sync roda logo na
/// subida: é uma única chamada HTTP leve, e sem isso o cache ficaria vazio (preços caindo para USD)
/// até completar o primeiro intervalo inteiro depois de todo deploy.
/// </summary>
public sealed class ExchangeRateSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExchangeRateSyncBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public ExchangeRateSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeRateSyncBackgroundService> logger,
        TimeSpan interval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunSyncAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new SyncExchangeRateCommand(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Scheduled exchange rate sync failed.");
        }
    }
}

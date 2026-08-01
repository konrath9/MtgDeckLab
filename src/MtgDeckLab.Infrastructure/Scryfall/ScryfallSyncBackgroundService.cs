using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Cards.Commands.SyncScryfallCards;

namespace MtgDeckLab.Infrastructure.Scryfall;

/// <summary>
/// Roda o sync de bulk data da Scryfall periodicamente. Espera um intervalo antes do primeiro
/// disparo — sync imediato sob demanda já existe via POST /api/admin/sync-cards.
/// </summary>
public sealed class ScryfallSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScryfallSyncBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public ScryfallSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScryfallSyncBackgroundService> logger,
        TimeSpan interval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            var result = await sender.Send(new SyncScryfallCardsCommand(), ct);

            _logger.LogInformation(
                "Scheduled Scryfall sync finished: {Processed} cards, {Errors} errors, {Duration}.",
                result.ProcessedCount, result.ErrorCount, result.Duration);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Scheduled Scryfall sync failed.");
        }
    }
}

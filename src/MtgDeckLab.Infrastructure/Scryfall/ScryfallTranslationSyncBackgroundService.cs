using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Cards.Commands.SyncCardTranslations;

namespace MtgDeckLab.Infrastructure.Scryfall;

/// <summary>
/// Roda o sync de nomes traduzidos periodicamente. Como o de cartas, espera um intervalo antes do
/// primeiro disparo — sync sob demanda existe via POST /api/admin/sync-card-translations.
/// </summary>
public sealed class ScryfallTranslationSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScryfallTranslationSyncBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly IReadOnlyCollection<string> _languages;

    public ScryfallTranslationSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScryfallTranslationSyncBackgroundService> logger,
        TimeSpan interval,
        IReadOnlyCollection<string> languages)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
        _languages = languages;
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
            var result = await sender.Send(
                new SyncCardTranslationsCommand(_languages.Count > 0 ? _languages : null), ct);

            _logger.LogInformation(
                "Scheduled card translation sync finished: {Processed} read, {Applied} applied, {Errors} errors, {Duration}.",
                result.ProcessedCount, result.AppliedCount, result.ErrorCount, result.Duration);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Scheduled card translation sync failed.");
        }
    }
}

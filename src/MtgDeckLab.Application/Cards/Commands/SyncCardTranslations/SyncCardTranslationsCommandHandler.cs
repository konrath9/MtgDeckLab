using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Localization;

namespace MtgDeckLab.Application.Cards.Commands.SyncCardTranslations;

public class SyncCardTranslationsCommandHandler
    : IRequestHandler<SyncCardTranslationsCommand, SyncCardTranslationsResult>
{
    private const int BatchSize = 500;

    private readonly IScryfallSyncService _scryfallService;
    private readonly ICardRepository _cardRepo;
    private readonly ILogger<SyncCardTranslationsCommandHandler> _logger;

    public SyncCardTranslationsCommandHandler(
        IScryfallSyncService scryfallService,
        ICardRepository cardRepo,
        ILogger<SyncCardTranslationsCommandHandler> logger)
    {
        _scryfallService = scryfallService;
        _cardRepo = cardRepo;
        _logger = logger;
    }

    public async Task<SyncCardTranslationsResult> Handle(
        SyncCardTranslationsCommand request,
        CancellationToken cancellationToken)
    {
        var languages = request.Languages is { Count: > 0 }
            ? request.Languages
            : CardLanguage.Translatable;

        var sw = Stopwatch.StartNew();
        var batch = new List<CardTranslation>(BatchSize);
        int processed = 0, applied = 0, errors = 0;

        try
        {
            await foreach (var translation in
                _scryfallService.StreamCardTranslationsAsync(languages, cancellationToken))
            {
                batch.Add(translation);
                if (batch.Count < BatchSize) continue;

                var (batchApplied, ok) = await TryFlushBatchAsync(batch, cancellationToken);
                if (ok) { processed += batch.Count; applied += batchApplied; } else errors++;
                batch.Clear();

                _logger.LogInformation(
                    "Card translation sync progress: {Processed} read, {Applied} applied.",
                    processed, applied);
            }

            if (batch.Count > 0)
            {
                var (batchApplied, ok) = await TryFlushBatchAsync(batch, cancellationToken);
                if (ok) { processed += batch.Count; applied += batchApplied; } else errors++;
            }
        }
        catch (Exception ex)
        {
            // Mesmo contrato do sync de cartas: erro aqui vem do stream (conexão caiu) e encerra o
            // sync; falha de um lote específico é tratada em TryFlushBatchAsync e não interrompe.
            _logger.LogError(ex, "Card translation sync failed after {Processed} translations.", processed);
            errors++;
        }

        sw.Stop();
        _logger.LogInformation(
            "Card translation sync complete: {Processed} read, {Applied} applied in {Elapsed}. Errors: {Errors}.",
            processed, applied, sw.Elapsed, errors);

        return new SyncCardTranslationsResult(processed, applied, errors, sw.Elapsed);
    }

    private async Task<(int Applied, bool Ok)> TryFlushBatchAsync(
        List<CardTranslation> batch, CancellationToken ct)
    {
        try
        {
            return (await _cardRepo.UpsertTranslationsAsync(batch, ct), true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Card translation sync: failed to upsert batch of {Count}, skipping.", batch.Count);
            return (0, false);
        }
    }
}

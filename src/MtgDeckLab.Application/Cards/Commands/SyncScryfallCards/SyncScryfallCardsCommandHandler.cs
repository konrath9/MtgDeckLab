using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Application.Cards.Commands.SyncScryfallCards;

public class SyncScryfallCardsCommandHandler
    : IRequestHandler<SyncScryfallCardsCommand, SyncScryfallCardsResult>
{
    private const int BatchSize = 500;

    private readonly IScryfallSyncService _scryfallService;
    private readonly ICardRepository _cardRepo;
    private readonly ILogger<SyncScryfallCardsCommandHandler> _logger;

    public SyncScryfallCardsCommandHandler(
        IScryfallSyncService scryfallService,
        ICardRepository cardRepo,
        ILogger<SyncScryfallCardsCommandHandler> logger)
    {
        _scryfallService = scryfallService;
        _cardRepo = cardRepo;
        _logger = logger;
    }

    public async Task<SyncScryfallCardsResult> Handle(
        SyncScryfallCardsCommand request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var batch = new List<Card>(BatchSize);
        int processed = 0, errors = 0;

        try
        {
            await foreach (var card in _scryfallService.StreamOracleCardsAsync(cancellationToken))
            {
                batch.Add(card);

                if (batch.Count >= BatchSize)
                {
                    if (await TryFlushBatchAsync(batch, cancellationToken)) processed += batch.Count;
                    else errors++;
                    batch.Clear();
                    _logger.LogInformation("Scryfall sync progress: {Count} cards processed.", processed);
                }
            }

            if (batch.Count > 0)
            {
                if (await TryFlushBatchAsync(batch, cancellationToken)) processed += batch.Count;
                else errors++;
            }
        }
        catch (Exception ex)
        {
            // Um erro aqui vem do stream em si (ex.: conexão HTTP caiu) — não dá pra continuar.
            // Falha de um lote específico (ex.: violação de constraint) é tratada em
            // TryFlushBatchAsync e não interrompe o resto do sync.
            _logger.LogError(ex, "Scryfall sync failed after {Count} cards.", processed);
            errors++;
        }

        sw.Stop();
        _logger.LogInformation(
            "Scryfall sync complete: {Count} cards in {Elapsed}. Errors: {Errors}.",
            processed, sw.Elapsed, errors);

        return new SyncScryfallCardsResult(processed, errors, sw.Elapsed);
    }

    private async Task<bool> TryFlushBatchAsync(List<Card> batch, CancellationToken ct)
    {
        try
        {
            await _cardRepo.UpsertManyAsync(batch, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scryfall sync: failed to upsert batch of {Count} cards, skipping.", batch.Count);
            return false;
        }
    }
}

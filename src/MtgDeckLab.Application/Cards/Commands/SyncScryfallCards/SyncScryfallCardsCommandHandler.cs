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
                    await FlushBatchAsync(batch, cancellationToken);
                    processed += batch.Count;
                    batch.Clear();
                    _logger.LogInformation("Scryfall sync progress: {Count} cards processed.", processed);
                }
            }

            if (batch.Count > 0)
            {
                await FlushBatchAsync(batch, cancellationToken);
                processed += batch.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scryfall sync failed after {Count} cards.", processed);
            errors++;
        }

        sw.Stop();
        _logger.LogInformation(
            "Scryfall sync complete: {Count} cards in {Elapsed}. Errors: {Errors}.",
            processed, sw.Elapsed, errors);

        return new SyncScryfallCardsResult(processed, errors, sw.Elapsed);
    }

    private async Task FlushBatchAsync(List<Card> batch, CancellationToken ct) =>
        await _cardRepo.UpsertManyAsync(batch, ct);
}

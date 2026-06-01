using MediatR;

namespace MtgDeckLab.Application.Cards.Commands.SyncScryfallCards;

public record SyncScryfallCardsCommand : IRequest<SyncScryfallCardsResult>;

public record SyncScryfallCardsResult(int ProcessedCount, int ErrorCount, TimeSpan Duration);

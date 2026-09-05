using MediatR;

namespace MtgDeckLab.Application.ExchangeRates.Commands.SyncExchangeRate;

public record SyncExchangeRateCommand : IRequest<SyncExchangeRateResult>;

public record SyncExchangeRateResult(bool Success, decimal? UsdToBrl, DateTimeOffset? FetchedAt);

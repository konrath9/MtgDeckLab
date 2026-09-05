using MediatR;

namespace MtgDeckLab.Application.ExchangeRates.Queries.GetExchangeRate;

public record GetExchangeRateQuery : IRequest<ExchangeRateResponse>;

/// <summary>
/// Campos nulos quando nenhum sync rodou ainda com sucesso (ex.: logo após o primeiro deploy,
/// antes do sync inicial completar, ou fonte externa fora do ar). O cliente cai para USD nesse caso.
/// </summary>
public record ExchangeRateResponse(decimal? UsdToBrl, DateTimeOffset? AsOf);

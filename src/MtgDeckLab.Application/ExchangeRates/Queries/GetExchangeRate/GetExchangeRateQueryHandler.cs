using MediatR;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.ExchangeRates.Queries.GetExchangeRate;

public class GetExchangeRateQueryHandler : IRequestHandler<GetExchangeRateQuery, ExchangeRateResponse>
{
    private readonly IExchangeRateStore _store;

    public GetExchangeRateQueryHandler(IExchangeRateStore store) => _store = store;

    public Task<ExchangeRateResponse> Handle(GetExchangeRateQuery request, CancellationToken cancellationToken)
    {
        var current = _store.Current;
        return Task.FromResult(new ExchangeRateResponse(current?.UsdToBrl, current?.FetchedAt));
    }
}

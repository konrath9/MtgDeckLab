using MediatR;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.ExchangeRates.Queries.GetExchangeRate;

namespace MtgDeckLab.API.Controllers;

/// <summary>
/// Cotação USD→BRL cacheada, sincronizada diariamente — ver
/// <c>MtgDeckLab.Infrastructure.ExchangeRates.ExchangeRateSyncBackgroundService</c>. O frontend
/// usa isto para converter e exibir preços em R$ quando o idioma é pt-BR.
/// </summary>
[ApiController]
[Route("api/exchange-rate")]
public class ExchangeRatesController : ControllerBase
{
    private readonly ISender _sender;

    public ExchangeRatesController(ISender sender) => _sender = sender;

    /// <summary>
    /// Retorna a cotação USD→BRL cacheada. Campos nulos quando nenhum sync completou ainda —
    /// o cliente deve cair para USD nesse caso.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ExchangeRateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _sender.Send(new GetExchangeRateQuery(), ct);
        return Ok(result);
    }
}

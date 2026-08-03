using MediatR;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Cards.Queries.SearchCards;
using MtgDeckLab.Application.Common;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/cards")]
public class CardsController : ControllerBase
{
    private readonly ISender _sender;

    public CardsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Busca cartas sincronizadas da Scryfall por nome, tipo, faixa de CMC ou set, com paginação.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CardSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? type,
        [FromQuery] decimal? minCmc,
        [FromQuery] decimal? maxCmc,
        [FromQuery] string? setCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new SearchCardsQuery(name, type, minCmc, maxCmc, setCode, page, pageSize), ct);
        return Ok(result);
    }
}

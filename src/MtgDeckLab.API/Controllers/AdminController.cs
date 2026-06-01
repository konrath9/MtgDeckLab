using MediatR;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Cards.Commands.SyncScryfallCards;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender) => _sender = sender;

    /// <summary>
    /// Baixa o bulk data da Scryfall e sincroniza a tabela de cartas.
    /// </summary>
    [HttpPost("sync-cards")]
    [ProducesResponseType(typeof(SyncScryfallCardsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncCards(CancellationToken ct)
    {
        var result = await _sender.Send(new SyncScryfallCardsCommand(), ct);
        return Ok(result);
    }
}

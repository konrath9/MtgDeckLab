using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Cards.Commands.SyncCardTranslations;
using MtgDeckLab.Application.Cards.Commands.SyncScryfallCards;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
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

    /// <summary>
    /// Baixa o bulk multilíngue da Scryfall e grava os nomes das cartas nos idiomas pedidos.
    /// </summary>
    /// <param name="languages">
    /// Códigos Scryfall separados por vírgula (ex.: <c>pt</c>). Vazio = todos os idiomas
    /// traduzíveis que a aplicação conhece.
    /// </param>
    /// <remarks>
    /// É um download de vários GB (o bulk multilíngue traz toda impressão em todo idioma), então
    /// roda sob demanda e num agendamento próprio — ver <c>Scryfall:Translations</c>.
    /// </remarks>
    [HttpPost("sync-card-translations")]
    [ProducesResponseType(typeof(SyncCardTranslationsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncCardTranslations(
        [FromQuery] string? languages, CancellationToken ct)
    {
        var requested = (languages ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var result = await _sender.Send(
            new SyncCardTranslationsCommand(requested.Count > 0 ? requested : null), ct);

        return Ok(result);
    }
}

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Decks.Commands.ImportDeck;
using MtgDeckLab.Application.Decks.Commands.TakeFinanceSnapshot;
using MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;
using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Decks.Queries.GetDeckFinanceSummary;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/decks")]
[Authorize]
public class DecksController : ControllerBase
{
    private readonly ISender _sender;

    public DecksController(ISender sender) => _sender = sender;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Retorna detalhes de um deck do usuário autenticado.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeckDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDeckByIdQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Importa um deck a partir de texto no formato Moxfield/Archidekt.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportDeckResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromBody] ImportDeckRequest request, CancellationToken ct)
    {
        var command = new ImportDeckCommand(
            request.Name, request.Format, request.Decklist, CurrentUserId, request.Description);

        var result = await _sender.Send(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.DeckId },
            new ImportDeckResponse(result.DeckId, result.ResolvedCards, result.UnresolvedCardNames));
    }

    /// <summary>Retorna a análise completa do deck: mana curve, cores, tipos, validação e score.</summary>
    [HttpGet("{id:guid}/analysis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysis(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new AnalyzeDeckQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Retorna o resumo financeiro do deck e histórico de snapshots.</summary>
    [HttpGet("{id:guid}/finance")]
    [ProducesResponseType(typeof(DeckFinanceSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinance(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDeckFinanceSummaryQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Registra um snapshot do custo atual do deck.</summary>
    [HttpPost("{id:guid}/finance/snapshot")]
    [ProducesResponseType(typeof(TakeFinanceSnapshotResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TakeSnapshot(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new TakeFinanceSnapshotCommand(id, CurrentUserId), ct);
            return Created($"/api/decks/{id}/finance", result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public record ImportDeckRequest(
    string Name,
    Format Format,
    string Decklist,
    string? Description = null
);

public record ImportDeckResponse(
    Guid DeckId,
    int ResolvedCards,
    IReadOnlyList<string> UnresolvedCardNames
);

using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Common;
using MtgDeckLab.Application.Decks.Commands.DeleteDeck;
using MtgDeckLab.Application.Decks.Commands.ImportDeck;
using MtgDeckLab.Application.Decks.Commands.TakeDeckVersion;
using MtgDeckLab.Application.Decks.Commands.TakeFinanceSnapshot;
using MtgDeckLab.Application.Decks.Commands.UpdateDeck;
using MtgDeckLab.Application.Decks.Commands.UpsertDeckEntry;
using MtgDeckLab.Application.Decks.Queries.AnalyzeDeck;
using MtgDeckLab.Application.Decks.Queries.GetDeckById;
using MtgDeckLab.Application.Decks.Queries.GetDeckFinanceSummary;
using MtgDeckLab.Application.Decks.Queries.GetDeckVersionById;
using MtgDeckLab.Application.Decks.Queries.GetDeckVersionDiff;
using MtgDeckLab.Application.Decks.Queries.GetDeckRecommendations;
using MtgDeckLab.Application.Decks.Queries.GetDeckSimulation;
using MtgDeckLab.Application.Decks.Queries.ListDecks;
using MtgDeckLab.Application.Decks.Queries.ListDeckVersions;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Domain.Exceptions;
using MtgDeckLab.Domain.Enums;
using MtgDeckLab.Engine.Analysis.Models;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/decks")]
[Authorize]
public class DecksController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IApiMessageLocalizer _messages;

    public DecksController(ISender sender, IApiMessageLocalizer messages)
    {
        _sender = sender;
        _messages = messages;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lista os decks do usuário autenticado, com paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DeckSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _sender.Send(new ListDecksQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Retorna detalhes de um deck do usuário autenticado.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeckDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDeckByIdQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Renomeia/atualiza a descrição de um deck do usuário autenticado.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DeckDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeckRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateDeckCommand(id, CurrentUserId, request.Name, request.Description), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Remove um deck do usuário autenticado (e seu histórico financeiro).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _sender.Send(new DeleteDeckCommand(id, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Define a quantidade de uma carta num slot do deck (main/sideboard/commander).
    /// Quantity = 0 remove a carta.
    /// </summary>
    [HttpPut("{id:guid}/entries")]
    [ProducesResponseType(typeof(UpsertDeckEntryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertEntry(Guid id, [FromBody] UpsertDeckEntryRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new UpsertDeckEntryCommand(
                id, CurrentUserId, request.CardName, request.Quantity, request.Section), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (CardNotFoundException ex)
        {
            // O nome procurado volta no texto traduzido: quem digitou "Ilah" precisa ver o que
            // não foi encontrado, no idioma em que está usando a aplicação.
            return BadRequest(new
            {
                error = _messages.Get(ApiMessageCodes.CardNotFound, ("card", ex.CardName)),
                code = ApiMessageCodes.CardNotFound
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return BadRequest(new { error = ex.Message });
        }
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
            request.Name, request.Format, request.MainDecklist, CurrentUserId,
            request.CommanderDecklist, request.SideboardDecklist, request.MaybeboardDecklist,
            request.Description);

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

    /// <summary>
    /// Recomenda cartas do banco (sem IA) pra cada papel que a matriz de cobertura marcou como
    /// carente (Red) — ranqueadas por color identity, papéis extras e proximidade da CMC média.
    /// </summary>
    [HttpGet("{id:guid}/recommendations")]
    [ProducesResponseType(typeof(DeckRecommendations), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDeckRecommendationsQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Simula (Monte Carlo, embaralhando o deck N vezes) taxa de mão mantível e disponibilidade
    /// de cada papel por turno. Resultado é estocástico — não confundir com /analysis, que é
    /// determinístico.
    /// </summary>
    [HttpGet("{id:guid}/simulation")]
    [ProducesResponseType(typeof(MonteCarloSimulationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulation(
        Guid id, [FromQuery] int iterations = 10_000, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDeckSimulationQuery(id, CurrentUserId, iterations), ct);
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

    /// <summary>Registra uma nova versão do deck: snapshot da composição atual + score calculado no momento.</summary>
    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(TakeDeckVersionResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TakeVersion(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new TakeDeckVersionCommand(id, CurrentUserId), ct);
            return Created($"/api/decks/{id}/versions/{result.VersionId}", result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Lista o histórico de versões do deck, da mais recente para a mais antiga.</summary>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<DeckVersionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListVersions(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new ListDeckVersionsQuery(id, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Retorna a composição completa de uma versão específica do deck.</summary>
    [HttpGet("{id:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(DeckVersionDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDeckVersionByIdQuery(id, versionId, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Compara duas versões do deck: cartas adicionadas/removidas/com quantidade alterada,
    /// variação de score, CMC média e custo total (preços atuais).
    /// </summary>
    [HttpGet("{id:guid}/versions/diff")]
    [ProducesResponseType(typeof(DeckVersionDiff), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DiffVersions(
        Guid id, [FromQuery] Guid fromVersionId, [FromQuery] Guid toVersionId, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetDeckVersionDiffQuery(id, fromVersionId, toVersionId, CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record ImportDeckRequest(
    string Name,
    Format Format,
    string MainDecklist,
    string? CommanderDecklist = null,
    string? SideboardDecklist = null,
    string? MaybeboardDecklist = null,
    string? Description = null
);

public record ImportDeckResponse(
    Guid DeckId,
    int ResolvedCards,
    IReadOnlyList<UnresolvedCardName> UnresolvedCardNames
);

public record UpdateDeckRequest(string Name, string? Description = null);

public record UpsertDeckEntryRequest(
    string CardName,
    int Quantity,
    DeckSection Section = DeckSection.Main
);

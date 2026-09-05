using MediatR;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Auth.Commands.Login;
using MtgDeckLab.Application.Auth.Commands.Register;
using MtgDeckLab.Application.Localization;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IApiMessageLocalizer _messages;

    public AuthController(ISender sender, IApiMessageLocalizer messages)
    {
        _sender = sender;
        _messages = messages;
    }

    /// <summary>Cria uma nova conta. Retorna um JWT válido por 24h.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] AuthRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new RegisterUserCommand(request.Email, request.Password), ct);
            return StatusCode(StatusCodes.Status201Created, new AuthResponse(result.UserId, result.Token));
        }
        catch (InvalidOperationException)
        {
            // A exceção do handler é diagnóstico (log); o que o usuário lê vem do catálogo, no
            // idioma da requisição.
            return Conflict(ErrorPayload(ApiMessageCodes.EmailAlreadyRegistered));
        }
    }

    /// <summary>Autentica com email e senha. Retorna um JWT válido por 24h.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return Ok(new AuthResponse(result.UserId, result.Token));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ErrorPayload(ApiMessageCodes.InvalidCredentials));
        }
    }

    // "error" é o texto já traduzido (o que a UI mostra) e "code" é a chave estável — um cliente
    // que queira traduzir por conta própria não depende da frase.
    private object ErrorPayload(string code) =>
        new { error = _messages.Get(code), code };
}

public record AuthRequest(string Email, string Password);
public record AuthResponse(Guid UserId, string Token);

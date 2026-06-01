using MediatR;
using Microsoft.AspNetCore.Mvc;
using MtgDeckLab.Application.Auth.Commands.Login;
using MtgDeckLab.Application.Auth.Commands.Register;

namespace MtgDeckLab.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
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
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }
}

public record AuthRequest(string Email, string Password);
public record AuthResponse(Guid UserId, string Token);

using MediatR;

namespace MtgDeckLab.Application.Auth.Commands.Register;

public record RegisterUserCommand(string Email, string Password) : IRequest<AuthTokenResult>;

public record AuthTokenResult(Guid UserId, string Token);

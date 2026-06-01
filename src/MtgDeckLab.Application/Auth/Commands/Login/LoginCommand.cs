using MediatR;
using MtgDeckLab.Application.Auth.Commands.Register;

namespace MtgDeckLab.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokenResult>;

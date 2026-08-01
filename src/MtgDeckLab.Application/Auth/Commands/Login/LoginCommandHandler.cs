using MediatR;
using MtgDeckLab.Application.Auth.Commands.Register;
using MtgDeckLab.Application.Interfaces;

namespace MtgDeckLab.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public LoginCommandHandler(
        IUserRepository userRepo, IPasswordHasher hasher, IJwtService jwt)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthTokenResult(user.Id, _jwt.GenerateToken(user.Id, user.Email, user.Role));
    }
}

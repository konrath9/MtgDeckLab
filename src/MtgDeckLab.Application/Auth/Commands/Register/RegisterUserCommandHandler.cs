using MediatR;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Auth.Commands.Register;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthTokenResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IAdminEmailAllowlist _adminAllowlist;

    public RegisterUserCommandHandler(
        IUserRepository userRepo, IPasswordHasher hasher, IJwtService jwt, IAdminEmailAllowlist adminAllowlist)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _jwt = jwt;
        _adminAllowlist = adminAllowlist;
    }

    public async Task<AuthTokenResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var role = _adminAllowlist.IsAdmin(request.Email) ? Role.Admin : Role.User;
        var user = new User(request.Email, _hasher.Hash(request.Password), role);
        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(user.Id, _jwt.GenerateToken(user.Id, user.Email, user.Role));
    }
}

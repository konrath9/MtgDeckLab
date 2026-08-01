using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email, Role role);
}

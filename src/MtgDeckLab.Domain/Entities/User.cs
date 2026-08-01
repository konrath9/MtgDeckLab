using MtgDeckLab.Domain.Enums;

namespace MtgDeckLab.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private init; }
    public string Email { get; private init; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }

    private User() { }

    public User(string email, string passwordHash, Role role = Role.User)
    {
        Id = Guid.NewGuid();
        Email = email.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

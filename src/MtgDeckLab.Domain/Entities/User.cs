namespace MtgDeckLab.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private init; }
    public string Email { get; private init; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }

    private User() { }

    public User(string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email.ToLowerInvariant().Trim();
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

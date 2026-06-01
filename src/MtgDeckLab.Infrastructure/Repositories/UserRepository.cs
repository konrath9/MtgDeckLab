using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Domain.Entities;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MtgDeckLabDbContext _context;

    public UserRepository(MtgDeckLabDbContext context) => _context = context;

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}

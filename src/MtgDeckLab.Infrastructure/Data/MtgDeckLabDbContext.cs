using Microsoft.EntityFrameworkCore;
using MtgDeckLab.Domain.Entities;

namespace MtgDeckLab.Infrastructure.Data;

public class MtgDeckLabDbContext : DbContext
{
    public MtgDeckLabDbContext(DbContextOptions<MtgDeckLabDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckEntry> DeckEntries => Set<DeckEntry>();
    public DbSet<FinanceSnapshot> FinanceSnapshots => Set<FinanceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MtgDeckLabDbContext).Assembly);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MtgDeckLab.Infrastructure.Data;

// Used by EF Core CLI tools (dotnet ef migrations add / database update)
public class MtgDeckLabDbContextFactory : IDesignTimeDbContextFactory<MtgDeckLabDbContext>
{
    public MtgDeckLabDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MtgDeckLabDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=mtgdecklab;Username=mtgdecklab;Password=mtgdecklab_dev")
            .Options;

        return new MtgDeckLabDbContext(options);
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using MtgDeckLab.Infrastructure.Data;

namespace MtgDeckLab.API.Tests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("mtgdecklab_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MtgDeckLabDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configuração injetada antes do Program.cs rodar — sobrepõe appsettings
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "test-secret-key-must-be-32-chars-long!!",
                ["Jwt:Issuer"] = "MtgDeckLab",
                ["Jwt:Audience"] = "MtgDeckLab",
                ["Jwt:ExpiresInHours"] = "1",
                ["Admin:Emails"] = "admin@test.com"
            });
        });

        // Substituir o DbContext para apontar ao container de teste
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MtgDeckLabDbContext>));

            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<MtgDeckLabDbContext>(opt =>
                opt.UseNpgsql(_postgres.GetConnectionString()));
        });

        builder.UseEnvironment("Testing");
    }
}

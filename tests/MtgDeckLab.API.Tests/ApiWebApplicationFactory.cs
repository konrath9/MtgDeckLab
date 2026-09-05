using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
        // UseSetting, não ConfigureAppConfiguration+AddInMemoryCollection: Program.cs usa hosting
        // mínimo (WebApplication.CreateBuilder) e lê algumas dessas chaves de forma síncrona e
        // antecipada dentro de AddInfrastructure (ex.: o gate de ScheduledSyncEnabled, decidido
        // no momento do registro do serviço, não só quando ele efetivamente roda). Uma fonte
        // adicionada via ConfigureAppConfiguration só entra depois desse ponto — o valor lido ali
        // ainda seria o do appsettings.json. UseSetting entra na configuração de host bem mais
        // cedo (a mesma camada usada por variável de ambiente/linha de comando), a tempo de valer
        // pra esses reads antecipados. (Descoberto quando o sync de câmbio — que, ao contrário do
        // da Scryfall, dispara imediatamente — vazou uma chamada HTTP real nos testes mesmo com
        // ScheduledSyncEnabled=false aqui.)
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "test-secret-key-must-be-32-chars-long!!");
        builder.UseSetting("Jwt:Issuer", "MtgDeckLab");
        builder.UseSetting("Jwt:Audience", "MtgDeckLab");
        builder.UseSetting("Jwt:ExpiresInHours", "1");
        builder.UseSetting("Admin:Emails", "admin@test.com");
        builder.UseSetting("Scryfall:ScheduledSyncEnabled", "false");
        builder.UseSetting("ExchangeRates:ScheduledSyncEnabled", "false");

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

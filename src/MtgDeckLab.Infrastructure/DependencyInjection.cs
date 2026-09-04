using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Infrastructure.Auth;
using MtgDeckLab.Infrastructure.Data;
using MtgDeckLab.Infrastructure.Repositories;
using MtgDeckLab.Infrastructure.Scryfall;

namespace MtgDeckLab.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MtgDeckLabDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<IDeckVersionRepository, DeckVersionRepository>();
        services.AddScoped<IFinanceSnapshotRepository, FinanceSnapshotRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminEmailAllowlist, ConfigAdminEmailAllowlist>();

        services.AddHttpClient<IScryfallSyncService, ScryfallSyncService>(client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MtgDeckLab/1.0 (portfolio project)");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        var scheduledSyncEnabled = !bool.TryParse(configuration["Scryfall:ScheduledSyncEnabled"], out var enabled) || enabled;
        if (scheduledSyncEnabled)
        {
            var intervalHours = double.TryParse(configuration["Scryfall:SyncIntervalHours"], out var h) ? h : 24;

            services.AddHostedService(sp => new ScryfallSyncBackgroundService(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ScryfallSyncBackgroundService>>(),
                TimeSpan.FromHours(intervalHours)));
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                var secret = configuration["Jwt:Secret"]!;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

        services.AddAuthorizationBuilder();

        return services;
    }
}

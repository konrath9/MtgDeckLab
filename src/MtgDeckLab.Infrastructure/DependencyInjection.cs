using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MtgDeckLab.Application.Interfaces;
using MtgDeckLab.Application.Localization;
using MtgDeckLab.Infrastructure.Auth;
using MtgDeckLab.Infrastructure.Data;
using MtgDeckLab.Infrastructure.Localization;
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

        AddLocalizationServices(services, configuration);

        services.AddHttpClient<IScryfallSyncService, ScryfallSyncService>(client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MtgDeckLab/1.0 (portfolio project)");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        AddScheduledSyncs(services, configuration);

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

    private static void AddLocalizationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Os .resx vivem em Resources/Localization/ deste projeto; o tipo-âncora do catálogo
        // (ex.: Localization.AnalysisMessages) completa o caminho do arquivo.
        services.AddLocalization(opt => opt.ResourcesPath = "Resources");

        services.AddSingleton(ReadLocalizationOptions(configuration));
        services.AddSingleton<ILanguageContext, CurrentCultureLanguageContext>();
        services.AddScoped<IAnalysisMessageLocalizer, ResourceAnalysisMessageLocalizer>();
        services.AddScoped<IApiMessageLocalizer, ResourceApiMessageLocalizer>();
    }

    /// <summary>
    /// Lê a seção <c>Localization</c>. Aceita tanto lista ("Localization:SupportedCultures:0")
    /// quanto string separada por vírgula, que é o formato prático para variável de ambiente.
    /// </summary>
    public static AppLocalizationOptions ReadLocalizationOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(AppLocalizationOptions.SectionName);
        var defaults = new AppLocalizationOptions();

        var cultures = section.GetSection(nameof(AppLocalizationOptions.SupportedCultures))
            .GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .ToList();

        if (cultures.Count == 0)
        {
            cultures = (section[nameof(AppLocalizationOptions.SupportedCultures)] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        if (cultures.Count == 0) cultures = defaults.SupportedCultures.ToList();

        var defaultCulture = section[nameof(AppLocalizationOptions.DefaultCulture)];
        if (string.IsNullOrWhiteSpace(defaultCulture) || !cultures.Contains(defaultCulture))
            defaultCulture = cultures.Contains(defaults.DefaultCulture) ? defaults.DefaultCulture : cultures[0];

        return new AppLocalizationOptions
        {
            DefaultCulture = defaultCulture,
            SupportedCultures = cultures
        };
    }

    private static void AddScheduledSyncs(IServiceCollection services, IConfiguration configuration)
    {
        var scheduledSyncEnabled = !bool.TryParse(configuration["Scryfall:ScheduledSyncEnabled"], out var enabled) || enabled;
        if (!scheduledSyncEnabled) return;

        var intervalHours = double.TryParse(configuration["Scryfall:SyncIntervalHours"], out var h) ? h : 24;

        services.AddHostedService(sp => new ScryfallSyncBackgroundService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<ScryfallSyncBackgroundService>>(),
            TimeSpan.FromHours(intervalHours)));

        // O sync de traduções é opt-in e tem seu próprio intervalo: ele baixa o bulk multilíngue
        // da Scryfall (vários GB), enquanto o de cartas baixa só o bulk em inglês. Nomes
        // traduzidos também mudam muito menos que preço, então rodar junto seria desperdício.
        var translationsEnabled = bool.TryParse(configuration["Scryfall:Translations:Enabled"], out var t) && t;
        if (!translationsEnabled) return;

        var translationIntervalHours =
            double.TryParse(configuration["Scryfall:Translations:SyncIntervalHours"], out var th) ? th : 24 * 7;

        var languages = (configuration["Scryfall:Translations:Languages"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        services.AddHostedService(sp => new ScryfallTranslationSyncBackgroundService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<ScryfallTranslationSyncBackgroundService>>(),
            TimeSpan.FromHours(translationIntervalHours),
            languages));
    }
}

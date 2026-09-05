using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgDeckLab.Infrastructure;
using MtgDeckLab.Infrastructure.ExchangeRates;

namespace MtgDeckLab.API.Tests.Unit;

public class ExchangeRateSchedulingTests
{
    private static readonly Dictionary<string, string?> BaseConfig = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x;Username=x;Password=x",
        ["Jwt:Secret"] = "test-secret-key-must-be-32-chars-long!!",
        ["Jwt:Issuer"] = "MtgDeckLab",
        ["Jwt:Audience"] = "MtgDeckLab",
        ["Scryfall:ScheduledSyncEnabled"] = "false"
    };

    private static IServiceCollection BuildServices(IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>(BaseConfig);
        if (overrides is not null)
            foreach (var (key, value) in overrides)
                values[key] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services;
    }

    [Fact]
    public void AddInfrastructure_ByDefault_RegistersScheduledExchangeRateSync()
    {
        var services = BuildServices();
        using var provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>().Should().Contain(s => s is ExchangeRateSyncBackgroundService);
    }

    [Fact]
    public void AddInfrastructure_WithScheduledSyncExplicitlyDisabled_DoesNotRegisterHostedService()
    {
        var services = BuildServices(new Dictionary<string, string?>
        {
            ["ExchangeRates:ScheduledSyncEnabled"] = "false"
        });
        using var provider = services.BuildServiceProvider();

        provider.GetServices<IHostedService>().Should().NotContain(s => s is ExchangeRateSyncBackgroundService);
    }
}

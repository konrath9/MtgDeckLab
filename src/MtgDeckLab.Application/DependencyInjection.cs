using Microsoft.Extensions.DependencyInjection;
using MtgDeckLab.Engine.Analysis;
using MtgDeckLab.Engine.Parsing;

namespace MtgDeckLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddSingleton<DecklistParser>();
        services.AddSingleton<DeckAnalyzer>();

        return services;
    }
}

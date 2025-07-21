using App.Application.Abstractions;
using App.Domain.Competition;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Infrastructure.PluginsRegistration;

namespace App.Web.DependencyInjection;

public static class PluginsDependencyInjection
{
    public static IServiceCollection AddPluginsInfrastructure(
        this IServiceCollection services)
    {
        services
            .AddSingleton<ICompetitionEnginePluginRepository,
                Infrastructure.ApplicationRepository.CompetitionEnginePlugin.InMemory>();

        services.AddHostedService<PluginRegistrationService>();

        AddDefaultPlugins(services);

        return services;
    }

    private static void AddDefaultPlugins(this IServiceCollection services)
    {
        services.AddSingleton<ICompetitionEnginePlugin>(serviceProvider =>
        {
            // TODO; Trzeba stworzyć mapper dla skoczni. Na razie nazwa + hs? Globalny mapper.
            var guid = serviceProvider.GetRequiredService<IGuid>();
            var factory = new Plugin.Engine.Classic.Factory((hill => 6.5), (hill => 10.8), (hill => 18.2), guid);
            return new Plugin.Plugins.Classic.Plugin(factory);
        });
    }
}
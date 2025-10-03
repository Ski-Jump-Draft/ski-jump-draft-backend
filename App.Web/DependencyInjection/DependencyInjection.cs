using App.Application.Utility;
using App.Web.DependencyInjection.Local;
using App.Web.DependencyInjection.Production;
using App.Web.DependencyInjection.Shared;

namespace App.Web.DependencyInjection;

public enum Mode
{
    Offline,
    Online
}

public static class DependencyInjection
{
    public static IServiceCollection InjectDependencies(this IServiceCollection services, IConfiguration config,
        Mode mode, IMyLogger? logger)
    {
        services.AddAcl().AddApplication().AddCommanding().AddMappers().AddStorages()
            .AddUtilities().AddBot().AddJson();
        services.AddMemoryCache();

        switch (mode)
        {
            case Mode.Offline:
                services.AddLocalApplication().AddLocalGame().AddLocalHostedServices().AddLocalMatchmaking()
                    .AddLocalNotifiers().AddLocalSimulation().AddLocalMyPlayer().AddLocalRepositories()
                    .AddLocalArchives(); break;
            case Mode.Online:
                var isMocked = config["ProductionDependencyInjection:IsMocked"] == "true";
                services.AddProductionApplication(isMocked: isMocked).AddProductionGame()
                    .AddProductionHostedServices(isMocked: isMocked)
                    .AddProductionMatchmaking(isMocked: isMocked)
                    .AddProductionNotifiers().AddProductionSimulation(isMocked: isMocked)
                    .AddGameWorld(config, isMocked: isMocked)
                    .AddProductionRepositories(config, isMocked: isMocked, logger: logger)
                    .AddProductionArchives(isMocked: isMocked);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        return services;
    }
}
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
    public static IServiceCollection InjectDependencies(this IServiceCollection services, Mode mode)
    {
        services.AddAcl().AddApplication().AddArchives().AddCommanding().AddMappers().AddRepositories().AddStorages()
            .AddUtilities().AddBot().AddJson();

        switch (mode)
        {
            case Mode.Offline:
                services.AddLocalApplication().AddLocalGame().AddLocalHostedServices().AddLocalMatchmaking()
                    .AddLocalNotifiers().AddLocalSimulation().AddLocalMyPlayer(); break;
            case Mode.Online:
                services.AddProductionApplication().AddProductionGame().AddProductionHostedServices()
                    .AddProductionMatchmaking()
                    .AddProductionNotifiers().AddProductionSimulation();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        return services;
    }
}
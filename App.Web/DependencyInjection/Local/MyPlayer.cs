using App.Application.OfflineTests;

namespace App.Web.DependencyInjection.Local;

public static class MyPlayer
{
    public static IServiceCollection AddLocalMyPlayer(this IServiceCollection services)
    {
        services.AddSingleton<App.Application.OfflineTests.IMyPlayer, DefaultMyPlayer>();
        return services;
    }
}
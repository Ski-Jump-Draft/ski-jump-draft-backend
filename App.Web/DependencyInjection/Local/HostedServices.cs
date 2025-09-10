using App.Web.HostedServices.MockedFlow;

namespace App.Web.DependencyInjection.Local;

public static class HostedServices
{
    public static IServiceCollection AddLocalHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<OfflineBotJoiner>();
        return services;
    }
}
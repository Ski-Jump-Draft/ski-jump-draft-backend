using App.Web.HostedServices.MockedFlow;

namespace App.Web.DependencyInjection.Production;

public static class HostedServices
{
    public static IServiceCollection AddProductionHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<BotJoiner>();
        return services;
    }
}


using App.Web.HostedServices.MockedFlow;
using App.Web.HostedServices.RealFlow;

namespace App.Web.DependencyInjection.Production;

public static class HostedServices
{
    public static IServiceCollection AddProductionHostedServices(this IServiceCollection services, bool isMocked)
    {
        if (isMocked)
        {
            // services.AddHostedService<MockedOnlineBotJoiner>();
            services.AddHostedService<App.Web.HostedServices.InternalTest.BotJoiner>();
        }
        else
        {
            services.AddHostedService<App.Web.HostedServices.InternalTest.BotJoiner>();
            // services.AddHostedService<OnlineBotJoiner>();
        }

        return services;
    }
}
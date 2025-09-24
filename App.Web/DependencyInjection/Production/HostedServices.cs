using App.Web.HostedServices.MockedFlow;
using App.Web.HostedServices.RealFlow;

namespace App.Web.DependencyInjection.Production;

public static class HostedServices
{
    public static IServiceCollection AddProductionHostedServices(this IServiceCollection services, bool isMocked)
    {
        if (isMocked)
        {
            services.AddHostedService<MockedOnlineBotJoiner>();
        }
        else
        {
            services.AddHostedService<OnlineBotJoiner>();
        }
        
        return services;
    }
}
using App.Domain.Shared;
using App.Domain.Time;

namespace App.Web.DependencyInjection;

public static class UtilitiesDependencyInjection
{
    public static IServiceCollection AddUtilities(
        this IServiceCollection services)
    {
        services.AddSingleton<IGuid, Infrastructure.Utility.DefaultGuid>();
        services.AddSingleton<IClock, Infrastructure.Utility.SystemClock>();

        return services;
    }
}
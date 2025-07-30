using App.Domain.Shared;
using App.Domain.Time;
using Random = App.Domain.Shared.Random;

namespace App.Web.DependencyInjection;

public static class UtilitiesDependencyInjection
{
    public static IServiceCollection AddUtilities(
        this IServiceCollection services)
    {
        services.AddSingleton<Random.IRandom, Infrastructure.Utility.SystemRandom>();
        services.AddSingleton<IGuid, Infrastructure.Utility.SystemGuid>();
        services.AddSingleton<IClock, Infrastructure.Utility.SystemClock>();

        return services;
    }
}
using App.Application.Utility;

namespace App.Web.DependencyInjection.Shared;

public static class Utilities
{
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IGuid, App.Infrastructure.Utility.Guid.SystemGuid>();
        services.AddSingleton<IClock, App.Infrastructure.Utility.Clock.SystemClock>();
        services.AddSingleton<IRandom, App.Infrastructure.Utility.Random.SystemRandom>();
        services.AddSingleton<IJson, App.Infrastructure.Utility.Json.DefaultJson>();
        services.AddSingleton<IMyLogger, App.Infrastructure.Utility.Logger.Dotnet>();
        return services;
    }
}
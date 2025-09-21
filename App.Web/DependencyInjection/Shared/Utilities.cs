using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Infrastructure.Utility.GameUpdatedDto;

namespace App.Web.DependencyInjection.Shared;

public static class Utilities
{
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IGuid, Infrastructure.Utility.GuidUtilities.SystemGuid>();
        services.AddSingleton<IClock, Infrastructure.Utility.Clock.SystemClock>();
        services.AddSingleton<IRandom, App.Infrastructure.Utility.Random.SystemRandom>();
        services.AddSingleton<IJson, App.Infrastructure.Utility.Json.DefaultJson>();
        services.AddSingleton<IMyLogger, Infrastructure.Utility.Logger.Dotnet>();
        services.AddSingleton<IGameUpdatedDtoMapperCache, MapperInMemoryCache>();
        return services;
    }
}
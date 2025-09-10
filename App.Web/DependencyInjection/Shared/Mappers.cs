using App.Application.Messaging.Notifiers.Mapper;

namespace App.Web.DependencyInjection.Shared;

public static class Mappers
{
    public static IServiceCollection AddMappers(this IServiceCollection services)
    {
        services
            .AddSingleton<GameUpdatedDtoMapper, GameUpdatedDtoMapper>();
        return services;
    }
}
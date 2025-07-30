using App.Application.Abstractions;

namespace App.Web.DependencyInjection;

public static class CommandsInfrastructureDependencyInjection
{
    public static IServiceCollection AddCommandsInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<ICommandBus, Infrastructure.CommandBus.InMemory>();

        return services;
    }
}
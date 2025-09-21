using App.Application.Bot;

namespace App.Web.DependencyInjection.Shared;

public static class Bot
{
    public static IServiceCollection AddBot(this IServiceCollection services)
    {
        services.AddSingleton<IBotRegistry, App.Infrastructure.Bot.Registry.InMemory>();
        services.AddSingleton<IBotPassPickLock, App.Infrastructure.Bot.PassPickLock.InMemory>();
        return services;
    }
}
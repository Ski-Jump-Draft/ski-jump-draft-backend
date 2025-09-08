using App.Application.Commanding;

namespace App.Web.DependencyInjection.Shared;

public static class Commanding
{
    public static IServiceCollection AddCommanding(this IServiceCollection services)
    {
        services.AddSingleton<ICommandBus, App.Infrastructure.Commanding.CommandBus.InMemory>();
        services.AddSingleton<IScheduler, App.Infrastructure.Commanding.Scheduler.InMemory>();
        return services;
    }
}
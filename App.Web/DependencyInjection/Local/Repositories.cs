namespace App.Web.DependencyInjection.Local;

public static class Repositories
{
    public static IServiceCollection AddLocalRepositories(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                App.Infrastructure.Repository.Matchmaking.InMemory>();
        services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();
        return services;
    }
}
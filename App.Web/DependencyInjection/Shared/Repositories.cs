using System.Globalization;
using CsvHelper.Configuration;

namespace App.Web.DependencyInjection.Shared;

public static class Repositories
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddSingleton<App.Domain.Matchmaking.IMatchmakings,
                App.Infrastructure.Repository.Matchmaking.InMemory>();
        services.AddSingleton<App.Domain.Game.IGames, App.Infrastructure.Repository.Game.InMemory>();
        return services;
    }
}
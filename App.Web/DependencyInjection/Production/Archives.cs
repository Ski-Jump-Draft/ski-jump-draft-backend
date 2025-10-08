namespace App.Web.DependencyInjection.Production;

public static class Archives
{
    public static IServiceCollection AddProductionArchives(this IServiceCollection services, bool isMocked)
    {
        if (isMocked)
        {
            services
                .AddSingleton<App.Application.Game.DraftPicks.IDraftPicksArchive,
                    App.Infrastructure.Archive.DraftPicks.InMemory>();
            services
                .AddSingleton<App.Application.Game.GameCompetitions.IGameCompetitionResultsArchive, App.Infrastructure.
                    Archive.
                    GameCompetitionResults.
                    InMemory>();
        }
        else
        {
            services
                .AddSingleton<App.Application.Game.DraftPicks.IDraftPicksArchive,
                    App.Infrastructure.Archive.DraftPicks.Redis>();
            services
                .AddSingleton<App.Application.Game.GameCompetitions.IGameCompetitionResultsArchive, App.Infrastructure.
                    Archive.
                    GameCompetitionResults.
                    Redis>();
        }

        services.AddSingleton<App.Application.Game.GameWind.IGameWind, App.Infrastructure.GameWind.InMemory>();

        return services;
    }
}
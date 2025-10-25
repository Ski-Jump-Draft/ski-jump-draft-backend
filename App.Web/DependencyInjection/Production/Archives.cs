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
            services
                .AddSingleton<App.Application.Game.DraftTurnIndexes.IDraftTurnIndexesArchive,
                    App.Infrastructure.Archive.DraftTurnIndexes.InMemoryDraftTurnIndexesArchive>();
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
            // No Redis implementation yet for DraftTurnIndexes; use in-memory for now.
            services
                .AddSingleton<App.Application.Game.DraftTurnIndexes.IDraftTurnIndexesArchive,
                    App.Infrastructure.Archive.DraftTurnIndexes.InMemoryDraftTurnIndexesArchive>();
        }

        services.AddSingleton<App.Application.Game.GameWind.IGameWind, Infrastructure.GameWind.InMemory>();
        services
            .AddSingleton<App.Application.Game.PassPicksCount.IDraftPassPicksCountArchive,
                Infrastructure.Archive.DraftPassPicksCount.InMemory>();

        return services;
    }
}
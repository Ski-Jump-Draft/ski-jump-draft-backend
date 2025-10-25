namespace App.Web.DependencyInjection.Local;

public static class Archives
{
    public static IServiceCollection AddLocalArchives(this IServiceCollection services)
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
        services
            .AddSingleton<App.Application.Game.PassPicksCount.IDraftPassPicksCountArchive,
                Infrastructure.Archive.DraftPassPicksCount.InMemory>();
        return services;
    }
}
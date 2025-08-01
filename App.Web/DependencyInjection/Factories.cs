using App.Application.CompetitionEngine;
using App.Application.CompetitionEngine.Helper.HillMapping;
using App.Application.UseCase.Helper;

namespace App.Web.DependencyInjection;

public static class FactoriesDependencyInjection
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        //services.AddSingleton<IGameHillMapping, InMemoryGameHillMapping>();
        services.AddSingleton<IPreDraftHillMapping, InMemoryPreDraftHillMapping>();
        services.AddSingleton<ICompetitionHillMapping, InMemoryCompetitionHillMapping>();

        //services.AddSingleton<IGameHillFactory, Application.Factory.Impl.GameHill.Default>();
        services.AddSingleton<IPreDraftCompetitionHillFactory, Application.CompetitionEngine.Impl.PreDraftHill.Default>();
        services.AddSingleton<ICompetitionHillFactory, Application.CompetitionEngine.Impl.CompetitionHill.Default>();

        services
            .AddSingleton<IMatchmakingParticipantFactory, Application.CompetitionEngine.Impl.MatchmakingParticipant.Default>();
        services.AddSingleton<IGameParticipantFactory, Application.CompetitionEngine.Impl.GameParticipant.Default>();

        return services;
    }
}
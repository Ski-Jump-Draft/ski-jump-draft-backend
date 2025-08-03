using App.Application.Factory;
using App.Application.UseCase.Helper;

namespace App.Web.DependencyInjection;

public static class FactoriesDependencyInjection
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        //services.AddSingleton<IGameHillMapping, InMemoryGameHillMapping>();
        // services.AddSingleton<IPreDraftHillMapping, InMemoryPreDraftHillMapping>();
        // services.AddSingleton<ICompetitionHillMapping, InMemoryCompetitionHillMapping>();

        //services.AddSingleton<IGameHillFactory, Application.Factory.Impl.GameHill.Default>();
        services
            .AddSingleton<IPreDraftCompetitionHillFactory, Application.Factory.Impl.PreDraftHill.Default>();
        services.AddSingleton<ICompetitionHillFactory, Application.Factory.Impl.CompetitionHill.Default>();

        services
            .AddSingleton<IMatchmakingParticipantFactory,
                Application.Factory.Impl.MatchmakingParticipant.Default>();
        services.AddSingleton<IGameParticipantsFactory, Application.Factory.Impl.GameParticipants.Default>();

        return services;
    }
}
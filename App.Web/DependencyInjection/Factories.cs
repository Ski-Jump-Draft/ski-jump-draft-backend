using App.Application.Factory;
using App.Application.Factory.Impl.HillMapping;

namespace App.Web.DependencyInjection;

public static class FactoriesDependencyInjection
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        //services.AddSingleton<IGameHillMapping, InMemoryGameHillMapping>();
        services.AddSingleton<IPreDraftHillMapping, InMemoryPreDraftHillMapping>();
        services.AddSingleton<ICompetitionHillMapping, InMemoryCompetitionHillMapping>();

        //services.AddSingleton<IGameHillFactory, Application.Factory.Impl.GameHill.Default>();
        services.AddSingleton<IPreDraftCompetitionHillFactory, Application.Factory.Impl.PreDraftHill.Default>();
        services.AddSingleton<ICompetitionHillFactory, Application.Factory.Impl.CompetitionHill.Default>();

        return services;
    }
}
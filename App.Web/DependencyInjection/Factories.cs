using App.Application.Factory;
using App.Application.Factory.Impl;
using App.Application.Factory.Impl.CompetitionHillFactory;
using App.Application.Factory.Impl.GameHillFactory;
using App.Application.Factory.Impl.HillMapping;

namespace App.Web.DependencyInjection;

public static class FactoriesDependencyInjection
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<IGameHillMapping, InMemoryGameHillMapping>();
        services.AddSingleton<ICompetitionHillMapping, InMemoryCompetitionHillMapping>();
        
        services.AddSingleton<IGameHillFactory, DefaultGameHillFactory>();
        services.AddSingleton<ICompetitionHillFactory, DefaultCompetitionHillFactory>();
        
        return services;
    }
}
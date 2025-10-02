using App.Application.Game.GameGateSelectionPack;
using App.Application.Game.GameGateSelectionPack.JuryBraveryFactory;
using App.Application.Game.GameSimulationPack;
using App.Application.Game.GameSimulationPack.JudgesSimulatorFactory;
using App.Application.Game.GameSimulationPack.JumpSimulatorFactory;
using App.Application.Game.GameSimulationPack.WeatherEngineFactory;
using App.Application.Policy.GameGateSelector;
using Random = App.Infrastructure.GameSimulationPack.WeatherEngineFactory.Random;

namespace App.Web.DependencyInjection.Production;

public static class Simulation
{
    public static IServiceCollection AddProductionSimulation(this IServiceCollection services, bool isMocked)
    {
        services
            .AddSingleton<IJumpSimulatorFactory, Infrastructure.GameSimulationPack.JumpSimulatorFactory.DefaultFixed>();
        services.AddSingleton<IWeatherEngineFactory, Random>();
        services
            .AddSingleton<IJudgesSimulatorFactory,
                Infrastructure.GameSimulationPack.JudgesSimulatorFactory.DefaultFixed>();

        services.AddSingleton<IGameSimulationPack, InMemory>();

        // services.AddSingleton<IJuryBraveryFactory, FixedBravery>(_ => new FixedBravery(JuryBravery.Medium));
        services.AddSingleton<IJuryBraveryFactory, RandomBravery>();
        services.AddSingleton<IGameGateSelectionPack, DefaultInMemory>();
        return services;
    }
}
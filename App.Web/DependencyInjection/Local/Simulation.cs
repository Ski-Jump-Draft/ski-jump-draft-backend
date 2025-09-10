using App.Application.Utility;
using App.Simulator.Simple;

namespace App.Web.DependencyInjection.Local;

public static class Simulation
{
    public static IServiceCollection AddLocalSimulation(this IServiceCollection services)
    {
        const double baseFormFactor = 2;
        services.AddSingleton<App.Simulator.Simple.SimulatorConfiguration>(sp =>
            new SimulatorConfiguration(SkillImpactFactor: 1.5, AverageBigSkill: 7,
                FlightToTakeoffRatio: 1, RandomAdditionsRatio: 1, TakeoffRatingPointsByForm: baseFormFactor * 0.9,
                FlightRatingPointsByForm: baseFormFactor * 1.1));
        services.AddSingleton<App.Domain.Simulation.IWeatherEngine, App.Simulator.Simple.WeatherEngine>(sp =>
            new WeatherEngine(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IMyLogger>(),
                ConfigurationPresetFactory.StableHeadwind));
        services.AddSingleton<App.Domain.Simulation.IJumpSimulator, App.Simulator.Simple.JumpSimulator>();
        services.AddSingleton<App.Domain.Simulation.IJudgesSimulator, App.Simulator.Simple.JudgesSimulator>();
        return services;
    }
}
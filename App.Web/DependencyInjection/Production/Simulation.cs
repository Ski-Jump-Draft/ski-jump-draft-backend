using App.Application.Utility;
using App.Simulator.Simple;

namespace App.Web.DependencyInjection.Production;

public static class Simulation
{
    public static IServiceCollection AddProductionSimulation(this IServiceCollection services, bool isMocked)
    {
        const double baseFormFactor = 2.5;
        services.AddSingleton<App.Simulator.Simple.SimulatorConfiguration>(sp =>
            new SimulatorConfiguration(SkillImpactFactor: 1.5, AverageBigSkill: 7,
                FlightToTakeoffRatio: 1, RandomAdditionsRatio: 0.9, TakeoffRatingPointsByForm: baseFormFactor * 0.9,
                FlightRatingPointsByForm: baseFormFactor * 1.1, DistanceSpreadByRatingFactor: 1.2,
                HsFlatteningStartRatio: 0.001, HsFlatteningStrength: 1.15));
        services.AddSingleton<App.Domain.Simulation.IWeatherEngine, App.Simulator.Simple.WeatherEngine>(sp =>
            new WeatherEngine(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IMyLogger>()
                // new Configuration(0.5, 0.1, 0.13)
                , ConfigurationPresetFactory.VeryStrongHeadwind));
        services.AddSingleton<App.Domain.Simulation.IJumpSimulator, App.Simulator.Simple.JumpSimulator>();
        services.AddSingleton<App.Domain.Simulation.IJudgesSimulator, App.Simulator.Simple.JudgesSimulator>();
        return services;
    }
}
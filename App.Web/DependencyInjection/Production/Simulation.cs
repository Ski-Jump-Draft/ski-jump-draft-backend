using App.Application.Utility;
using App.Simulator.Simple;

namespace App.Web.DependencyInjection.Production;

public static class Simulation
{
    public static IServiceCollection AddProductionSimulation(this IServiceCollection services)
    {
        const double baseFormFactor = 2;
        services.AddSingleton<App.Simulator.Simple.SimulatorConfiguration>(sp =>
            new SimulatorConfiguration(SkillImpactFactor: 1.5, AverageBigSkill: 7,
                FlightToTakeoffRatio: 1, RandomAdditionsRatio: 0.8, TakeoffRatingPointsByForm: baseFormFactor * 0.9,
                FlightRatingPointsByForm: baseFormFactor * 1.1));
        services.AddSingleton<App.Domain.Simulation.IWeatherEngine, App.Simulator.Simple.WeatherEngine>(sp =>
            new WeatherEngine(sp.GetRequiredService<IRandom>(), sp.GetRequiredService<IMyLogger>(),
                ConfigurationPresetFactory.TotalLottery));
        services.AddSingleton<App.Domain.Simulation.IJumpSimulator, App.Simulator.Simple.JumpSimulator>();
        services.AddSingleton<App.Domain.Simulation.IJudgesSimulator, App.Simulator.Simple.JudgesSimulator>();
        return services;
    }
}
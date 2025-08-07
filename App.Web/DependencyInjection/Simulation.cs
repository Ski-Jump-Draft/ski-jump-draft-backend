using App.Domain.Simulating;
using App.Simulator.Simple;

namespace App.Web.DependencyInjection;

public static class SimulationDependencyInjection
{
    public static IServiceCollection AddSimulation(
        this IServiceCollection services)
    {
        services.AddSingleton<ISimulator, Mock>();

        return services;
    }
}
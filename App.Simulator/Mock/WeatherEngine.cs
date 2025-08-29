using App.Application._2.Utility;
using App.Domain._2.Simulation;

namespace App.Simulator.Mock;

public class WeatherEngine(IRandom random) : IWeatherEngine
{
    public Wind GetWind()
    {
        return WindModule.create(random.RandomDouble(0.44, 1.22));
    }

    public void SimulateTime(TimeSpan time)
    {
        return;
    }
}
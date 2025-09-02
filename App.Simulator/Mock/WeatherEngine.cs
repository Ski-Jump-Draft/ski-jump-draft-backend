using System.Globalization;
using App.Application._2.Utility;
using App.Domain._2.Simulation;

namespace App.Simulator.Mock;

public class WeatherEngine(IRandom random, IMyLogger logger) : IWeatherEngine
{
    public Wind GetWind()
    {
        var wind = WindModule.create(random.RandomDouble(0.44, 1.22));
        logger.Debug("Generated wind: " + (WindModule.averaged(wind).ToString(CultureInfo.InvariantCulture)) + "");
        return wind;
    }

    public void SimulateTime(TimeSpan time)
    {
        return;
    }
}
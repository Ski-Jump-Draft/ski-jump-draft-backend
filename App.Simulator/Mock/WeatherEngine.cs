using System.Globalization;
using App.Application.Utility;
using App.Domain.Simulation;

namespace App.Simulator.Mock;

public class WeatherEngine(IRandom random, Wind? constWind, IMyLogger logger) : IWeatherEngine
{
    public Wind GetWind()
    {
        if (constWind != null) return constWind;
        var wind = WindModule.create(random.RandomDouble(0.44, 1.22));
        logger.Debug("Generated wind: " + (WindModule.averaged(wind).ToString(CultureInfo.InvariantCulture)) + "");
        return wind;
    }

    public void SimulateTime(TimeSpan time)
    {
        return;
    }
}
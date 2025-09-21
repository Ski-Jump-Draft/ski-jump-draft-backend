using System.Globalization;
using App.Application.Utility;
using App.Domain.Simulation;

namespace App.Simulator.Simple;

public record Configuration(double StartingWind, double StableWindChangeStdDev, double WindAdditionStdDev);

public static class ConfigurationPresetFactory
{
    public static Configuration StableTailwind => new(-1, 0.01, 0.09);
    public static Configuration StableHeadwind => new(1.2, 0.03, 0.15);
    public static Configuration VeryStrongHeadwind => new(2.4, 0.04, 0.2);
    public static Configuration TypicalEngelberg => new(-2.1, 0.016, 0.2);
    public static Configuration LotteryHeadwind => new(1.7, 0.2, 0.5);
    public static Configuration TotalLottery => new(0.5, 0.3, 1.8);
    public static Configuration StableNeutral => new(-0.03, 0.005, 0.02);
}


/// <summary>
/// A simple weather engine using minutes. Not recommended to use longer than ~200 minutes
/// </summary>
public class WeatherEngine : IWeatherEngine
{
    private double _currentBaseWindDouble;
    private int _minutes;
    private readonly Configuration _configuration;
    private readonly IRandom _random;
    private readonly IMyLogger _logger;

    public WeatherEngine(IRandom random, IMyLogger logger, Configuration configuration)
    {
        _random = random;
        _logger = logger;
        _configuration = configuration;
        _currentBaseWindDouble = configuration.StartingWind;
    }

    public Wind GetWind()
    {
        var windDouble = _currentBaseWindDouble + GenerateWindAddition();
        var wind = WindModule.create(windDouble);
        _logger.Debug($"Generated wind ({_minutes} minutes): " +
                      (WindModule.averaged(wind).ToString(CultureInfo.InvariantCulture)) + "");
        return wind;
    }

    private double GenerateWindAddition()
    {
        return _random.Gaussian(0, _configuration.WindAdditionStdDev);
    }

    public int Minutes => _minutes;

    public void SimulateTime(TimeSpan time)
    {
        var minutes = (int)Math.Floor(time.TotalMinutes);
        _logger.Debug($"Simulating {minutes} minutes");
        for (var i = 0; i < minutes; i++)
        {
            var change = _random.Gaussian(0, _configuration.StableWindChangeStdDev);
            _currentBaseWindDouble += change;
        }

        _minutes += minutes;
    }
}
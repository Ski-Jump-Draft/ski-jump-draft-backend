using App.Domain.Simulation;

namespace App.Application.Game.GameSimulationPack.WeatherEngineFactory;

public interface IWeatherEngineFactory
{
    IWeatherEngine Create();
}
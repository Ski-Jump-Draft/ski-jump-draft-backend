using App.Domain.Simulation;

namespace App.Application.Game.GameSimulationPack;

public record GameSimulationPack(
    IJumpSimulator JumpSimulator,
    IWeatherEngine WeatherEngine,
    IJudgesSimulator JudgesSimulator
);

public interface IGameSimulationPack
{
    GameSimulationPack GetFor(Guid gameId);
}
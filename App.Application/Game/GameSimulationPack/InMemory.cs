using System.Collections.Concurrent;
using App.Application.Game.GameSimulationPack.JudgesSimulatorFactory;
using App.Application.Game.GameSimulationPack.JumpSimulatorFactory;
using App.Application.Game.GameSimulationPack.WeatherEngineFactory;
using App.Domain.Simulation;

namespace App.Application.Game.GameSimulationPack;

public class InMemory(
    IJumpSimulatorFactory jumpSimulatorFactory,
    IWeatherEngineFactory weatherEngineFactory,
    IJudgesSimulatorFactory judgesSimulatorFactory) : IGameSimulationPack
{
    private readonly ConcurrentDictionary<Guid, GameSimulationPack> _packs = new();

    public GameSimulationPack GetFor(Guid gameId)
    {
        return _packs.GetOrAdd(gameId, CreatePackForGame);
    }

    private GameSimulationPack CreatePackForGame(Guid gameId)
    {
        return new GameSimulationPack(
            jumpSimulatorFactory.Create(),
            weatherEngineFactory.Create(),
            judgesSimulatorFactory.Create());
    }
}
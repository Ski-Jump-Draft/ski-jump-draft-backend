using App.Domain.Simulation;

namespace App.Application.Game.GameSimulationPack.JudgesSimulatorFactory;

public interface IJudgesSimulatorFactory
{
    IJudgesSimulator Create();
}
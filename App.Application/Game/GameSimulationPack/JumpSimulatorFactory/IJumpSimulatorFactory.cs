using App.Domain.Simulation;

namespace App.Application.Game.GameSimulationPack.JumpSimulatorFactory;

public interface IJumpSimulatorFactory
{
    IJumpSimulator Create();
}
using App.Application.Game.GameSimulationPack.JudgesSimulatorFactory;
using App.Application.Game.GameSimulationPack.JumpSimulatorFactory;
using App.Application.Utility;
using App.Domain.Simulation;
using App.Simulator.Simple;

namespace App.Infrastructure.GameSimulationPack.JudgesSimulatorFactory;

public class DefaultFixed(IRandom random, IMyLogger logger) : IJudgesSimulatorFactory
{
    public IJudgesSimulator Create()
    {
        return new JudgesSimulator(random, logger);
    }
}
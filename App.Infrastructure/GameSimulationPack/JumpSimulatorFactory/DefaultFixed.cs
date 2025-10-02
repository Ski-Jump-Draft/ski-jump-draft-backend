using App.Application.Game.GameSimulationPack.JumpSimulatorFactory;
using App.Application.Utility;
using App.Domain.Simulation;
using App.Simulator.Simple;

namespace App.Infrastructure.GameSimulationPack.JumpSimulatorFactory;

public class DefaultFixed(IMyLogger logger, IRandom random) : IJumpSimulatorFactory
{
    public IJumpSimulator Create()
    {
        const double baseFormFactor = 2.5;
        var configuration = new SimulatorConfiguration(SkillImpactFactor: 1.5, AverageBigSkill: 7,
            FlightToTakeoffRatio: 1, RandomAdditionsRatio: 0.9, TakeoffRatingPointsByForm: baseFormFactor * 0.9,
            FlightRatingPointsByForm: baseFormFactor * 1.1, DistanceSpreadByRatingFactor: 1.2,
            HsFlatteningStartRatio: 0.001, HsFlatteningStrength: 1.15);
        return new JumpSimulator(configuration, random, logger);
    }
}
using App.Application._2.Utility;
using App.Domain._2.Simulation;

namespace App.Simulator.Mock;

public class JumpSimulator(IRandom random) : IJumpSimulator
{
    public Jump Simulate(SimulationContext context)
    {
        var distance = DistanceModule.tryCreate(random.RandomDouble(110, 140)).Value;
        var landingRandom = random.RandomInt(0, 100);
        var landing = landingRandom switch
        {
            <= 1 => Landing.Fall,
            <= 2 => Landing.TouchDown,
            <= 5 => Landing.Parallel,
            _ => Landing.Telemark,
        };
        return new Jump(distance, landing);
    }
}
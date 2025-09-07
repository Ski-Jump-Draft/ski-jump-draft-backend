using System.Collections.Immutable;
using App.Application.Game.Gate;
using App.Application.Mapping;
using App.Domain.Simulation;

namespace App.Application.Policy.GameGateSelector;

public class IterativeSimulated(
    IJumpSimulator simulator,
    IWeatherEngine weatherEngine,
    JuryBravery juryBravery,
    IEnumerable<Domain.Simulation.Jumper> jumpers,
    Domain.Simulation.Hill hill
) : IGameStartingGateSelector
{
    public int Select(GameStartingGateSelectorContext context)
    {
        var simulationHill = hill;
        var simulationJumpers = jumpers.ToImmutableArray();
        var hsPoint = Domain.Simulation.HillModule.HsPointModule.value(simulationHill.HsPoint);

        const int maxTries = 50;
        const int startingGate = 0;
        var currentGate = startingGate;
        var tries = 0;

        var isGoingHigher = !SomeoneJumpedOverHs(currentGate);

        while (tries < maxTries)
        {
            tries++;
            if (isGoingHigher)
            {
                if (SomeoneJumpedOverHs(currentGate))
                {
                    currentGate--;
                    break;
                }

                currentGate++;
            }
            else
            {
                if (!SomeoneJumpedOverHs(currentGate))
                    break;
                currentGate--;
            }
        }

        if (tries >= maxTries)
            throw new MaxTriesExceededException(maxTries, $"Could not find suitable gate after {maxTries} tries");

        currentGate += juryBravery switch
        {
            JuryBravery.High => +1,
            JuryBravery.Low => -1,
            JuryBravery.Medium => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(juryBravery), juryBravery, null)
        };

        return currentGate;

        bool SomeoneJumpedOverHs(int gate)
        {
            foreach (var jumper in simulationJumpers)
            {
                var wind = weatherEngine.GetWind();
                var simCtx = new SimulationContext(Gate.NewGate(gate), jumper, simulationHill, wind);
                var result = simulator.Simulate(simCtx);
                var distance = DistanceModule.value(result.Distance);
                if (distance > hsPoint) return true;
            }

            return false;
        }
    }
}

public enum JuryBravery
{
    High,
    Medium,
    Low
}

public class MaxTriesExceededException(int maxTries, string? message = null) : Exception(message);
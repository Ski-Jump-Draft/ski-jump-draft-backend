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
) : IStartingGateSelector
{
    public int Select()
    {
        var simulationHill = hill;
        var simulationJumpers = jumpers.ToImmutableArray();
        var kPoint = Domain.Simulation.HillModule.KPointModule.value(simulationHill.KPoint);
        var hsPoint = Domain.Simulation.HillModule.HsPointModule.value(simulationHill.HsPoint);

        const int maxTries = 50;
        const int startingGate = 0;
        var currentGate = startingGate;
        var tries = 0;

        var isGoingHigher = !SomeoneJumpedOverHs(currentGate);

        var allowedOvershootsPercent = juryBravery switch
        {
            JuryBravery.VeryHigh => Math.Round((decimal)5 / 50, 2),
            JuryBravery.High => Math.Round((decimal)3 / 50, 2),
            JuryBravery.Medium => Math.Round((decimal)1 / 50, 2),
            JuryBravery.Low => Math.Round((decimal)0 / 50, 2),
            JuryBravery.VeryLow => Math.Round((decimal)0 / 50, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(juryBravery), juryBravery, null)
        };
        var allowedOvershoots = (int)Math.Floor(simulationJumpers.Length * allowedOvershootsPercent);

        while (tries < maxTries)
        {
            tries++;

            var overshoots = CountOvershoots(currentGate);

            if (isGoingHigher)
            {
                if (overshoots > allowedOvershoots)
                {
                    currentGate--;
                    break;
                }

                currentGate++;
            }
            else
            {
                if (overshoots <= allowedOvershoots)
                    break;
                currentGate--;
            }
        }


        if (tries >= maxTries)
            throw new MaxTriesExceededException(maxTries, $"Could not find suitable gate after {maxTries} tries");

        currentGate += juryBravery switch
        {
            JuryBravery.VeryLow => -2,
            JuryBravery.Low => -1,
            _ => 0
        };

        if (kPoint >= 180)
            currentGate--;

        currentGate--;

        return currentGate;

        int CountOvershoots(int gate)
        {
            var count = 0;
            foreach (var jumper in simulationJumpers)
            {
                var wind = weatherEngine.GetWind();
                var simCtx = new SimulationContext(Gate.NewGate(gate), jumper, simulationHill, wind);
                var result = simulator.Simulate(simCtx);
                if (DistanceModule.value(result.Distance) > hsPoint)
                    count++;
            }

            return count;
        }

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
    VeryHigh,
    High,
    Medium,
    Low,
    VeryLow
}

public class MaxTriesExceededException(int maxTries, string? message = null) : Exception(message);
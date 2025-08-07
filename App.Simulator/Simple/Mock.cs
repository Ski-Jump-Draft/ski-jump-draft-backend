using App.Domain.Simulating;

namespace App.Simulator.Simple;

public class Mock(Domain.Shared.Random.IRandom random) : ISimulator
{
    public Jump Simulate(Context context)
    {
        var tailwind = random.NextInt(0, 230) / 100.0;
        var windAverage = WindAverage.CreateTailwind(tailwind);
        var distance = (double)random.NextInt(120, 140);
        return new Jump(windAverage, DistanceModule.tryCreate(distance).ResultValue,
            Landing.Telemark);
    }
}
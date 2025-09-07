using App.Application.Game.Gate;
using App.Application.Policy.GameGateSelector;
using App.Simulator.Simple;
using App.Domain.Simulation;
using App.Infrastructure.Utility.Logger;
using App.Infrastructure.Utility.Random;
using App.Simulator.Mock;
using Microsoft.Extensions.Logging;
using HillModule = App.Domain.Simulation.HillModule;

namespace Playground.Simulation;

public static class Program
{
    private static double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count <= 1)
            return 0;

        var sumOfSquaredDifferences = values.Sum(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(sumOfSquaredDifferences / (values.Count - 1));
    }

    public static void Main(string[] args)
    {
        var random = new SystemRandom();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug); //.AddConsole();
        });

        var logger = new Dotnet(loggerFactory);

        var weatherEngine = new WeatherEngine(random, WindModule.create(0.2), logger);
        var jumpSimulator = new JumpSimulator(random, logger);
        var judgesSimulator = new JudgesSimulator(random, logger);

        var jumper = new Jumper(new JumperSkills(JumperSkillsModule.BigSkillModule.tryCreate(7).Value,
            JumperSkillsModule.BigSkillModule.tryCreate(7).Value,
            JumperSkillsModule.LandingSkillModule.tryCreate(8).Value, JumperSkillsModule.FormModule.tryCreate(5).Value,
            JumperSkillsModule.LikesHillPolicy.None));
        const double pointsPerGate = 7.56;
        const double pointsPerMeter = 1.8;
        const double metersByGate = pointsPerGate / pointsPerMeter;
        var hill = new Hill(HillModule.KPointModule.tryCreate(125).Value, HillModule.HsPointModule.tryCreate(140).Value,
            new HillSimulationData(HillModule.HsPointModule.tryCreate(140).Value,
                HillModule.MetersByGateModule.tryCreate(metersByGate).Value));

        var gateSelector = new IterativeSimulated(jumpSimulator, weatherEngine, JuryBravery.Low,
            [jumper, jumper, jumper, jumper, jumper, jumper], hill);

        var gate = gateSelector.Select(new GameStartingGateSelectorContext());
        Console.WriteLine($"Chosen gate no. {gate}");

        var distances = new List<double>();
        var winds = new List<double>();

        for (var i = 0; i < 300; i++)
        {
            var wind = weatherEngine.GetWind();
            var windDouble = WindModule.averaged(wind);
            var ctx = new SimulationContext(Gate.NewGate(gate), jumper, hill, wind);
            var jump = jumpSimulator.Simulate(ctx);

            var distanceDouble = DistanceModule.value(jump.Distance);

            distances.Add(distanceDouble);
            winds.Add(windDouble);

            Console.WriteLine(
                $"Jump: {jump.Distance}m + {jump.Landing} ({windDouble:F2}m/s))"
            );
        }

        // Calculate statistics
        var minDistance = distances.Min();
        var maxDistance = distances.Max();
        var averageDistance = distances.Average();
        var stdDevDistance = CalculateStdDev(distances, averageDistance);

        var minWind = winds.Min();
        var maxWind = winds.Max();
        var averageWind = winds.Average();
        var stdDevWind = CalculateStdDev(winds, averageWind);

        // Print statistics summary
        Console.WriteLine("\nJump Distance Statistics:");
        Console.WriteLine($"  Min: {minDistance:F2}m");
        Console.WriteLine($"  Max: {maxDistance:F2}m");
        Console.WriteLine($"  Avg: {averageDistance:F2}m");
        Console.WriteLine($"  StdDev: {stdDevDistance:F2}m");

        Console.WriteLine("\nWind Statistics:");
        Console.WriteLine($"  Min: {minWind:F2}m/s");
        Console.WriteLine($"  Max: {maxWind:F2}m/s");
        Console.WriteLine($"  Avg: {averageWind:F2}m/s");
        Console.WriteLine($"  StdDev: {stdDevWind:F2}m/s");

        Console.WriteLine("\nFinished simulation run.");
    }
}
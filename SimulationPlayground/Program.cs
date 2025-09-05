using App.Application.Game.Gate;
using App.Application.Policy.GameGateSelector;
using App.Simulator.Simple;
using App.Domain.Simulation;
using App.Infrastructure.Utility.Logger;
using App.Infrastructure.Utility.Random;
using App.Simulator.Mock;
using Microsoft.Extensions.Logging;
using HillModule = App.Domain.Simulation.HillModule;

namespace SimulationPlayground;

public static class Program
{
    public static void Main(string[] args)
    {
        var random = new SystemRandom();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug); //.AddConsole();
        });

        var logger = new Dotnet(loggerFactory);

        var weatherEngine = new WeatherEngine(random, logger);
        var jumpSimulator = new JumpSimulator(random, logger);
        var judgesSimulator = new JudgesSimulator(random, logger);

        var gateSelector = new IterativeSimulated(jumpSimulator, weatherEngine, JuryBravery.Low);

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

        var gateSelectorContext =
            new GameStartingGateSelectorContext([jumper, jumper, jumper, jumper, jumper, jumper], hill);
        var gate = gateSelector.Select(gateSelectorContext);
        Console.WriteLine($"Chosen gate no. {gate}");

        for (var i = 0; i < 300; i++)
        {
            var ctx = new SimulationContext(Gate.NewGate(gate), jumper, hill, weatherEngine.GetWind());
            var jump = jumpSimulator.Simulate(ctx);

            Console.WriteLine(
                $"Jump: {jump.Distance}m + {jump.Landing}"
            );
        }

        Console.WriteLine("Finished simulation run.");
    }
}
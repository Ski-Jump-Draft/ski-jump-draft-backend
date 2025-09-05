using App.Application.Utility;
using App.Domain.Simulation;

namespace App.Simulator.Simple;

public class JumpSimulator(IRandom random, IMyLogger logger) : IJumpSimulator
{
    public Jump Simulate(SimulationContext context)
    {
        var takeoffRating = CalculateTakeoffRating(context);
        var flightRating = CalculateFlightRating(context);
        var averageWind = WindModule.averaged(context.Wind);
        var distance = CalculateDistance(context, takeoffRating, flightRating, averageWind);
        var landing = GenerateLanding(context, distance);
        logger.Info($"Distance: {distance}, AverageWind: {averageWind}, Landing: {landing}");
        return new Jump(DistanceModule.tryCreate(distance).Value, landing);
    }

    private double CalculateTakeoffRating(SimulationContext context)
    {
        // Od 1 do 10
        var takeoffSkill = JumperSkillsModule.BigSkillModule.value(context.Jumper.Skills.Takeoff);
        // Od 1 do 10
        var form = JumperSkillsModule.FormModule.value(context.Jumper.Skills.Form);

        const int drawMax = 1000000;
        var drawNumber = random.RandomInt(1, drawMax);
        var randomAdditive = ((double)drawNumber / drawMax) switch
        {
            < 0.05 => CalculateGoodScenarioTakeoffRatingRandom(context),
            < 0.15 => CalculateBadScenarioTakeoffRatingRandom(context),
            _ => CalculateMediumTakeoffRatingRandom(context),
        };
        logger.Info($"TakeoffRandom: {randomAdditive}");
        // Od 1 do 100
        var takeoffRating = (takeoffSkill * 5) + (form * 7) + randomAdditive;

        return takeoffRating;
    }

    private double CalculateGoodScenarioTakeoffRatingRandom(SimulationContext context)
    {
        return random.Gaussian(8, 7);
    }

    private double CalculateBadScenarioTakeoffRatingRandom(SimulationContext context)
    {
        return random.Gaussian(-18, 10);
    }

    private double CalculateMediumTakeoffRatingRandom(SimulationContext context)
    {
        return random.Gaussian(0, 8);
    }

    private double CalculateFlightRating(SimulationContext context)
    {
        // Od 1 do 10
        var flightSkill = JumperSkillsModule.BigSkillModule.value(context.Jumper.Skills.Flight);
        // Od 1 do 10
        var form = JumperSkillsModule.FormModule.value(context.Jumper.Skills.Form);

        const int drawMax = 1000000;
        var drawNumber = random.RandomInt(1, drawMax);
        var randomAdditive = ((double)drawNumber / drawMax) switch
        {
            < 0.05 => CalculateGoodScenarioFlightRatingRandom(context),
            < 0.15 => CalculateBadScenarioFlightRatingRandom(context),
            _ => CalculateMediumFlightRatingRandom(context),
        };
        logger.Info($"FlightRandom: {randomAdditive}");
        // Od 1 do 100
        var rating = (flightSkill * 5 * 0.96) + (form * 7 * 1.04) + randomAdditive;

        return rating;
    }

    private double CalculateGoodScenarioFlightRatingRandom(SimulationContext context)
    {
        return random.Gaussian(8, 7);
    }

    private double CalculateBadScenarioFlightRatingRandom(SimulationContext context)
    {
        return random.Gaussian(-18, 10);
    }

    private double CalculateMediumFlightRatingRandom(SimulationContext context)
    {
        return random.Gaussian(0, 8);
    }

    private Landing GenerateLanding(SimulationContext context, double distance)
    {
        var realHs = HillModule.HsPointModule.value(context.Hill.SimulationData.RealHs);
        const int drawMax = 1000000;
        var drawNumber = random.RandomInt(1, drawMax);
        if (distance <= realHs)
        {
            return ((double)drawNumber / drawMax) switch
            {
                < 0.0005 => Landing.Fall,
                < 0.001 => Landing.TouchDown,
                < 0.01 => Landing.Parallel,
                _ => Landing.Telemark,
            };
        }

        if (distance <= realHs * 1.036) // do 145 metrów na RealHs = 140
        {
            return ((double)drawNumber / drawMax) switch
            {
                < 0.005 => Landing.Fall,
                < 0.01 => Landing.TouchDown,
                < 0.2 => Landing.Parallel,
                _ => Landing.Telemark
            };
        }

        if (distance <= realHs * 1.075) // do 150.5 metrów na RealHs = 140
        {
            return ((double)drawNumber / drawMax) switch
            {
                < 0.05 => Landing.Fall,
                < 0.10 => Landing.TouchDown,
                < 0.97 => Landing.Parallel,
                _ => Landing.Telemark
            };
        }

        return ((double)drawNumber / drawMax) switch // 151 metrów i wzwyż na RealHs = 140
        {
            < 0.7 => Landing.Fall,
            _ => Landing.TouchDown,
        };
    }

    private double CalculateDistance(SimulationContext context, double takeoffRating, double flightRating,
        double averageWind)
    {
        var metersByGate = HillModule.MetersByGateModule.value(context.Hill.SimulationData.MetersByGate);
        var gate = context.Gate.Item;

        var gateAddition = metersByGate * gate;
        logger.Info($"Gate: {gate}, MetersByGate: {metersByGate}, GateAddition: {
            gateAddition}");

        var kPoint = HillModule.KPointModule.value(context.Hill.KPoint);
        var startingDistance = kPoint / 2.5;
        var metersByTakeoffRating = 0.2 * (kPoint / 100);
        var metersByFlightRating = 0.2 * (kPoint / 100);
        logger.Info($"KPoint: {kPoint}, startingDistance: {startingDistance}, metersByTakeoffRating: {
            metersByTakeoffRating}, metersByFlightRating: {
                metersByFlightRating}");
        var takeoffAddition = metersByTakeoffRating * takeoffRating;
        var flightAddition = metersByFlightRating * flightRating;
        logger.Info($"TakeoffRating: {takeoffRating}, FlightRating: {flightRating}, TakeoffAddition: {takeoffAddition
        }, FlightAddition: {flightAddition}");
        var windAddition =
            averageWind * 4 *
            (kPoint / 50); // TODO: Dodać losowość większą, im większy wiatr. Dodać instability wiatru
        logger.Info($"Wind: {averageWind}, WindAddition: {windAddition}");
        return startingDistance + gateAddition + takeoffAddition + flightAddition + windAddition;
    }
}
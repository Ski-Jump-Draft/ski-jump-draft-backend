using App.Application.Utility;
using App.Domain.Simulation;

namespace App.Simulator.Simple;

/// <summary>
/// Simulator's config
/// </summary>
/// <param name="SkillImpactFactor">Increases the impact of differences between "big" skills (takeoff and flight skills). SkillImpactFactor=2 indicates that differences are two times more intensive. </param>
/// <param name="AverageBigSkill">Multiplies all random additions</param>
/// <param name="TakeoffRatingPointsByForm">How many takeoff rating points by a one point of the form?</param>
/// <param name="FlightRatingPointsByForm">How many flight rating points by a one point of the form?</param>
/// <param name="FlightToTakeoffRatio">E.g. 3.5 means that flight has 3.5 times more impact to distance than takeoff</param>
/// <param name="RandomAdditionsRatio">Multiplies all random additions</param>
/// <param name="DistanceSpreadByRatingFactor">Scales meters per rating point</param>
public record SimulatorConfiguration(
    double SkillImpactFactor,
    double AverageBigSkill,
    double TakeoffRatingPointsByForm,
    double FlightRatingPointsByForm,
    double FlightToTakeoffRatio = 1,
    double RandomAdditionsRatio = 1,
    double DistanceSpreadByRatingFactor = 1);

public class JumpSimulator(SimulatorConfiguration configuration, IRandom random, IMyLogger logger) : IJumpSimulator
{
    public Jump Simulate(SimulationContext context)
    {
        var takeoffRating = CalculateTakeoffRating(context);
        var flightRating = CalculateFlightRating(context);
        var averageWind = WindModule.averaged(context.Wind);
        var distance = CalculateDistance(context, takeoffRating, flightRating, averageWind);
        var landing = GenerateLanding(context, distance);
        logger.Debug($"Distance: {distance}, AverageWind: {averageWind}, Landing: {landing}");
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
        logger.Debug($"TakeoffRandom: {randomAdditive}");

        var baseRatingContribution = configuration.AverageBigSkill * 6;
        var skillDeviation = takeoffSkill - configuration.AverageBigSkill;
        var scaledDeviationContribution = skillDeviation * 6 * configuration.SkillImpactFactor;
        var formImpact = (form * configuration.TakeoffRatingPointsByForm);

        var takeoffRating = baseRatingContribution + scaledDeviationContribution + formImpact + randomAdditive;

        return takeoffRating;
    }

    private double CalculateGoodScenarioTakeoffRatingRandom(SimulationContext context)
    {
        return GenerateGaussianWithRandomAdditionsRatio(8, 7);
    }

    private double CalculateBadScenarioTakeoffRatingRandom(SimulationContext context)
    {
        return GenerateGaussianWithRandomAdditionsRatio(-18, 10);
    }

    private double CalculateMediumTakeoffRatingRandom(SimulationContext context)
    {
        return GenerateGaussianWithRandomAdditionsRatio(0, 8);
    }

    private double GenerateGaussianWithRandomAdditionsRatio(double mean, double stdDev)
    {
        return random.Gaussian(mean, stdDev) * configuration.RandomAdditionsRatio;
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
        logger.Debug($"FlightRandom: {randomAdditive}");

        var baseRatingContribution = configuration.AverageBigSkill * 6 * 0.96;
        var skillDeviation = flightSkill - configuration.AverageBigSkill;
        var scaledDeviationContribution = skillDeviation * 6 * 0.96 * configuration.SkillImpactFactor;
        var formImpact = (form * configuration.FlightRatingPointsByForm);

        var rating = baseRatingContribution + scaledDeviationContribution + formImpact + randomAdditive;

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
                < 0.009 => Landing.Parallel,
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
        logger.Debug($"Gate: {gate}, MetersByGate: {metersByGate}, GateAddition: {
            gateAddition}");

        var kPoint = HillModule.KPointModule.value(context.Hill.KPoint);
        var startingDistance = kPoint / 2.5;

        // ZMIANA: Upraszczamy bazowy przelicznik punktów ratingu na metry
        var metersByRatingPoint = 0.2 * (kPoint / 100) * configuration.DistanceSpreadByRatingFactor;
        logger.Debug($"KPoint: {kPoint}, startingDistance: {startingDistance}, metersByRatingPoint: {metersByRatingPoint
        }");

        var takeoffAddition = metersByRatingPoint * takeoffRating;
        var flightAddition = metersByRatingPoint * flightRating * configuration.FlightToTakeoffRatio;

        logger.Debug($"TakeoffRating: {takeoffRating}, FlightRating: {flightRating}, TakeoffAddition: {takeoffAddition
        }, FlightAddition: {flightAddition}");

        var windAddition = CalculateWindAddition(context, averageWind, kPoint);
        logger.Debug($"Wind: {averageWind}, WindAddition: {windAddition}");

        return startingDistance + gateAddition + takeoffAddition + flightAddition + windAddition;
    }

    private double CalculateWindAddition(SimulationContext context, double averageWind, double kPoint)
    {
        var windAddition = averageWind * 4 * (kPoint / 50);
        return windAddition;
    }
}
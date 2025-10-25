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
/// <param name="FlightToTakeoffRatio">E.g., 3.5 means that the flight has 3.5 times more impact to distance than takeoff</param>
/// <param name="RandomAdditionsRatio">Multiplies all random additions</param>
/// <param name="DistanceSpreadByRatingFactor">Scales meters per rating point</param>
/// <param name="HsFlatteningStartRatio">
/// Odsetek HS (np. 0.07 = 7%), gdzie zaczyna działać wypłaszczanie
/// </param>
/// <param name="HsFlatteningStrength">
/// Mnożnik siły wypłaszczania (1=normalnie, >1 = mocniej ścina, <1 = łagodniej)
/// </param>
public record SimulatorConfiguration(
    double SkillImpactFactor,
    double AverageBigSkill,
    double TakeoffRatingPointsByForm,
    double FlightRatingPointsByForm,
    double FlightToTakeoffRatio = 1,
    double RandomAdditionsRatio = 1,
    double DistanceSpreadByRatingFactor = 1,
    double HsFlatteningStartRatio = 0.07,
    double HsFlatteningStrength = 1.0);

public class JumpSimulator(SimulatorConfiguration configuration, IRandom random, IMyLogger logger) : IJumpSimulator
{
    public Jump Simulate(SimulationContext context)
    {
        var takeoffRating = CalculateTakeoffRating(context);
        var flightRating = CalculateFlightRating(context);
        var averageWind = WindModule.average(context.Wind);
        var windInstability = WindModule.instability(context.Wind);
        var rawDistance = CalculateDistance(context, takeoffRating, flightRating, averageWind, windInstability);
        var realHs = HillModule.HsPointModule.value(context.Hill.SimulationData.RealHs);
        var distanceAfterApplyingHsCost = ApplyHsCost(rawDistance, realHs);
        var landing = GenerateLanding(context, distanceAfterApplyingHsCost);
        logger.Debug($"Distance: {distanceAfterApplyingHsCost} (raw: {rawDistance}) AverageWind: {averageWind
        }, Landing: {landing}");
        return new Jump(DistanceModule.tryCreate(distanceAfterApplyingHsCost).Value, landing);
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
        return random.Gaussian(5, 5);
    }

    private double CalculateBadScenarioFlightRatingRandom(SimulationContext context)
    {
        return random.Gaussian(-15, 5);
    }

    private double CalculateMediumFlightRatingRandom(SimulationContext context)
    {
        return random.Gaussian(0, 6);
    }
    //
    // private static double DynamicFlightToTakeoffRatio(double k)
    // {
    //     return k switch
    //     {
    //         <= 50 => 1.0 / 5.0,
    //         <= 90 => Lerp(1.0 / 5.0, 3.0 / 5.0, SmoothStep(50, 90, k)),
    //         <= 110 => Lerp(2.0 / 5.0, 1, SmoothStep(90, 110, k)),
    //         <= 135 => Lerp(1, 1.5, SmoothStep(110, 135, k)),
    //         <= 200 => Lerp(1.5, 5, SmoothStep(125, 200, k)),
    //         _ => k / 40
    //     };
    // }

    private static double DynamicFlightToTakeoffRatio(double k)
    {
        return k switch
        {
            <= 50 => 0.2,
            <= 90 => 0.2 + (0.6 - 0.2) * ((k - 50) / 40.0),
            <= 110 => 0.6 + (1.0 - 0.6) * ((k - 90) / 20.0),
            <= 135 => 1.0 + (1.5 - 1.0) * ((k - 110) / 25.0),
            <= 200 => 1.5 + (5.0 - 1.5) * ((k - 135) / 65.0),
            _ => k / 40.0
        };
    }

    private static double BigHillSpreadAttenuation(double k)
    {
        // Większe BigHillSpreadAttenuation (np 0.54 -> 0.65)  = większe różnice
        return Lerp(1.0, 0.6, SmoothStep(160, 200, k));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

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
        double averageWind, double windInstability)
    {
        var metersByGate = HillModule.MetersByGateModule.value(context.Hill.SimulationData.MetersByGate);
        var gate = context.Gate.Item;

        var gateAddition = metersByGate * gate;
        logger.Debug($"Gate: {gate}, MetersByGate: {metersByGate}, GateAddition: {
            gateAddition}");

        var kPoint = HillModule.KPointModule.value(context.Hill.KPoint);
        var startingDistance = kPoint / 2.5;

        var baseMetersByRatingPoint = 0.2 * (kPoint / 100) * configuration.DistanceSpreadByRatingFactor;
        var bigHillSpreadAttenuation = BigHillSpreadAttenuation(kPoint);
        var metersByRatingPoint = baseMetersByRatingPoint * bigHillSpreadAttenuation;

        var takeoffAddition = metersByRatingPoint * takeoffRating;

        var flightToTakeoffRatio = DynamicFlightToTakeoffRatio(kPoint);
        var flightToTakeoffRatioAfterIncludingConfiguration =
            configuration.FlightToTakeoffRatio *
            flightToTakeoffRatio; // globalny mnożnik dalej działa (ustaw 1, by mieć dokładnie kotwice)
        var flightAddition = metersByRatingPoint * flightRating * flightToTakeoffRatioAfterIncludingConfiguration;

        logger.Debug($"FlightToTakeoffRatio(Dyn/Eff): {flightToTakeoffRatio}/{
            flightToTakeoffRatioAfterIncludingConfiguration}");

        logger.Debug($"TakeoffRating: {takeoffRating}, FlightRating: {flightRating}, TakeoffAddition: {takeoffAddition
        }, FlightAddition: {flightAddition}");

        var windAddition = CalculateWindAddition(averageWind, windInstability, kPoint);
        logger.Debug($"Wind: {averageWind}, WindAddition: {windAddition}");

        return startingDistance + gateAddition + takeoffAddition + flightAddition + windAddition;
    }

    private double CalculateWindAddition(double averageWind, double windInstability,
        double kPoint)
    {
        if (averageWind == 0) return 0;

        var perMsHeadwind = CalculatePerMsHeadwind(kPoint);
        perMsHeadwind *= SkiFlyingBoost(kPoint);

        var windAbs = Math.Abs(averageWind);

        var sigma = CalculateInstabilitySigma(windInstability, windAbs);
        var randomizedFactor = RandomizedFactor(random, sigma, windInstability);

        var metersLinear = windAbs * perMsHeadwind;
        var asym = averageWind < 0 ? -TailMultiplier(windAbs, windInstability) : 1.0;

        return metersLinear * asym * randomizedFactor;
    }

    /// <summary>
    /// Calculates meters per headwind m/s using some fancy AI's algorithm
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private static double CalculatePerMsHeadwind(double k)
    {
        const double a = 0.0078019484919;
        const double b = 1.38264025417;
        return a * Math.Pow(k, b);
    }

    /// <summary>
    /// Returns value from 0 to 1. K pertains to (185,inf). 250 or more is 1.
    /// </summary>
    /// <param name="k"></param>
    /// <returns></returns>
    private static double SkiFlyingBoost(double k)
    {
        return 1.0 + 0.08 * SmoothStep(185, 240, k);
    }

    private static double TailMultiplier(double wAbs, double instability)
    {
        var baseTail = 1.5 + 0.22 * SmoothStep(1.5, 3.5, wAbs);
        var strongWindAtt = 1.0 - 0.5 * SmoothStep(4, 10, wAbs);
        var stabilityAtt = 1.0 - 0.5 * Math.Clamp(instability, 0, 1);
        return 1.0 + (baseTail - 1.0) * strongWindAtt * stabilityAtt;
    }

    private static double CalculateInstabilitySigma(double instability, double wAbs)
    {
        return Math.Max(1e-6, (instability * 0.5) * (1.0 + 0.3 * SmoothStep(0, 6, wAbs)));
    }

    private static double RandomizedFactor(IRandom random, double sigma, double instability)
    {
        var rndVal = random.Gaussian(0, sigma);
        return Math.Clamp(1.0 + rndVal, 1.0 - instability, 1.0 + instability);
    }

    private static double SmoothStep(double edge0, double edge1, double x)
    {
        if (edge1 <= edge0) return x < edge0 ? 0 : 1;
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private double ApplyHsCost(double distance, double realHs)
    {
        if (distance <= realHs) return distance;

        var overHs = distance - realHs;

        var hsScale = configuration.HsFlatteningStartRatio <= 0
            ? 1e-6
            : realHs * configuration.HsFlatteningStartRatio;

        var compressed = hsScale * Math.Log(1.0 + overHs / hsScale);
        compressed /= configuration.HsFlatteningStrength;

        return realHs + compressed;
    }
}
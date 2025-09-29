using App.Application.Utility;
using App.Domain.Simulation;
using Microsoft.FSharp.Collections;

namespace App.Simulator.Simple;

public class JudgesSimulator(IRandom random, IMyLogger logger) : IJudgesSimulator
{
    public Judges Evaluate(JudgesSimulationContext context)
    {
        var landingSkill = JumperSkillsModule.LandingSkillModule.value(context.Jumper.Skills.Landing);
        const double noteAdditionByOneLandingSkill = 0.3;
        var baseNote =
            17.5
            + (landingSkill - 7) * noteAdditionByOneLandingSkill;

        logger.Debug($"Base Note after including landing skill: {baseNote}");
        baseNote = EnsureNoteRange(baseNote);
        baseNote = EnsureNoteRange(baseNote + JudgeNoteDistanceBaseBonus(context));
        logger.Debug($"Base Note after including distance bonus: {baseNote}");
        baseNote = EnsureNoteRange(baseNote + JudgeNoteBaseRandom(context));
        logger.Debug($"Base Note after including base random and landing kind: {baseNote}");

        var notes =
            Enumerable.Range(0, 5)
                .Select(i =>
                {
                    var note = EnsureNoteRange(baseNote + JudgeNoteSpecificRandom(context));
                    logger.Debug($"Note no. {i + 1}: {note}");
                    return note;
                })
                .ToList();
        try
        {
            return JudgesModule.tryCreate(ListModule.OfSeq(notes)).Value;
        }
        catch
        {
            throw new Exception($"Invalid notes format: {string.Join(",", notes)}");
        }
    }

    private static double EnsureNoteRange(double x) =>
        Math.Clamp(x, 0, 20);

    private double JudgeNoteBaseRandom(JudgesSimulationContext context)
    {
        return context.Jump.Landing.Tag switch
        {
            Landing.Tags.Telemark => random.RandomDouble(-0.7, 0.7),
            Landing.Tags.Parallel => random.RandomDouble(-3, -2),
            Landing.Tags.TouchDown => random.RandomDouble(-9, 7),
            Landing.Tags.Fall => random.RandomDouble(-11.5, -8.5),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private double JudgeNoteDistanceBaseBonus(JudgesSimulationContext context)
    {
        var k = HillModule.KPointModule.value(context.Hill.KPoint);
        var hs = HillModule.HsPointModule.value(context.Hill.HsPoint);
        var distance = DistanceModule.value(context.Jump.Distance);
        var distanceClampedToHs = Math.Min(distance, hs * 1.01); // 141,4 przy HS = 140
        const double
            kMultiplier =
                0.25; // N metrów zwiększa notę o (distance - k) / (k * kMultiplier). Mniejszy kMultiplier = większy wpływ odległości
        return (distanceClampedToHs - k) / (k * kMultiplier);
    }

    private double JudgeNoteSpecificRandom(JudgesSimulationContext context)
    {
        return context.Jump.Landing.Tag switch
        {
            Landing.Tags.Telemark => random.RandomDouble(-0.7, 0.7),
            Landing.Tags.Parallel => random.RandomDouble(-1.5, 1.5),
            Landing.Tags.TouchDown => random.RandomDouble(-2.4, 2.4),
            Landing.Tags.Fall => random.RandomDouble(-2, 2),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
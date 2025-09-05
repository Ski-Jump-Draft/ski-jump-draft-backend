using App.Application.Extensions;
using App.Domain.Competition;
using App.Domain.Simulation;
using HillModule = App.Domain.Simulation.HillModule;

namespace App.Application.Mapping;

public static class SimulationJumperMapper
{
    public static Domain.Simulation.Jumper ToSimulationJumper(this Domain.GameWorld.Jumper jumper, bool? likesHill)
    {
        var likesHillPolicy = likesHill switch
        {
            null => JumperSkillsModule.LikesHillPolicy.None,
            true => JumperSkillsModule.LikesHillPolicy.Likes,
            false => JumperSkillsModule.LikesHillPolicy.DoesNotLike,
        };
        var jumperSkills = new JumperSkills(
            JumperSkillsModule.BigSkillModule
                .tryCreate(Domain.GameWorld.JumperModule.BigSkillModule.value(jumper.Takeoff))
                .OrThrow("Wrong takeoff"),
            JumperSkillsModule.BigSkillModule
                .tryCreate(Domain.GameWorld.JumperModule.BigSkillModule.value(jumper.Flight))
                .OrThrow("Wrong flight"),
            JumperSkillsModule.LandingSkillModule
                .tryCreate(Domain.GameWorld.JumperModule.LandingSkillModule.value(jumper.Landing))
                .OrThrow($"Wrong landing ({jumper.Landing})"),
            JumperSkillsModule.FormModule
                .tryCreate(Domain.GameWorld.JumperModule.LiveFormModule.value(jumper.LiveForm))
                .OrThrow("Wrong live form"),
            likesHillPolicy);
        return new Domain.Simulation.Jumper(jumperSkills);
    }
}

public static class SimulationHillMapper
{
    public static Domain.Simulation.Hill ToSimulationHill(this Domain.GameWorld.Hill hill,
        double? overridenMetersByGate = null)
    {
        double metersByGate;
        if (overridenMetersByGate is not null)
        {
            metersByGate = overridenMetersByGate.Value;
        }
        else
        {
            var kPoint = Domain.GameWorld.HillModule.KPointModule.value(hill.KPoint);
            var pointsByMeter = HillPointsForMeterCalculator.calculate(kPoint);
            metersByGate = (Domain.GameWorld.HillModule.GatePointsModule.value(hill.GatePoints)) / pointsByMeter;
        }

        return new Domain.Simulation.Hill(
            HillModule.KPointModule
                .tryCreate(Domain.GameWorld.HillModule.KPointModule.value(hill.KPoint))
                .OrThrow("Wrong kpoint"),
            HillModule.HsPointModule
                .tryCreate(Domain.GameWorld.HillModule.HsPointModule.value(hill.HsPoint)).OrThrow("Wrong hs point"),
            new HillSimulationData(
                HillModule.HsPointModule.tryCreate(Domain.GameWorld.HillModule.HsPointModule.value(hill.HsPoint))
                    .OrThrow("Wrong real hs point"),
                HillModule.MetersByGateModule.tryCreate(metersByGate).OrThrow("Wrong meters by gate")));
    }

    public static Domain.Simulation.Hill ToSimulationHill(this Domain.Competition.Hill hill,
        double? overridenMetersByGate = null)
    {
        double metersByGate;
        if (overridenMetersByGate is not null)
        {
            metersByGate = overridenMetersByGate.Value;
        }
        else
        {
            var kPoint = Domain.Competition.HillModule.KPointModule.value(hill.KPoint);
            var pointsByMeter = HillPointsForMeterCalculator.calculate(kPoint);
            metersByGate = (Domain.Competition.HillModule.GatePointsModule.value(hill.GatePoints)) / pointsByMeter;
        }

        return new Domain.Simulation.Hill(
            HillModule.KPointModule
                .tryCreate(Domain.Competition.HillModule.KPointModule.value(hill.KPoint))
                .OrThrow("Wrong kpoint"),
            HillModule.HsPointModule
                .tryCreate(Domain.Competition.HillModule.HsPointModule.value(hill.HsPoint)).OrThrow("Wrong hs point"),
            new HillSimulationData(
                HillModule.HsPointModule.tryCreate(Domain.Competition.HillModule.HsPointModule.value(hill.HsPoint))
                    .OrThrow("Wrong real hs point"),
                HillModule.MetersByGateModule.tryCreate(metersByGate).OrThrow("Wrong meters by gate")));
    }
}
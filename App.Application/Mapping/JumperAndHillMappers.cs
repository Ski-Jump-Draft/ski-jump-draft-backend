using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Domain.Competition;
using App.Domain.GameWorld;
using App.Domain.Simulation;
using HillId = App.Domain.GameWorld.HillId;
using HillModule = App.Domain.Simulation.HillModule;
using JumperId = App.Domain.Competition.JumperId;

namespace App.Application.Mapping;

public static class JumperMapper
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

    public static IEnumerable<GameWorldJumperDto> ToGameWorldJumpers(
        this Domain.Game.Jumpers gameJumpers, IGameJumperAcl acl)
    {
        var idsEnumerable = Domain.Game.JumpersModule.toIdsList(gameJumpers);
        return idsEnumerable.Select(gameJumperId =>
        {
            var gameWorldJumperDto = acl.GetGameWorldJumper(gameJumperId.Item);
            return new GameWorldJumperDto(gameWorldJumperDto.Id);
        });
    }

    public static async Task<IEnumerable<Domain.GameWorld.Jumper>> ToGameWorldJumpers(
        this Domain.Game.Jumpers gameJumpers, IGameJumperAcl acl, IJumpers jumpers, CancellationToken ct = default)
    {
        var gameWorldJumperDtos = gameJumpers.ToGameWorldJumpers(acl);
        var gameWorldJumpers = await
            jumpers.GetFromIds(gameWorldJumperDtos.Select(dto => Domain.GameWorld.JumperId.NewJumperId(dto.Id)), ct);
        return gameWorldJumpers;
    }

    public static IEnumerable<Domain.Competition.Jumper> ToCompetitionJumpers(
        this Domain.Game.Jumpers gameJumpers, ICompetitionJumperAcl acl)
    {
        var idsEnumerable = Domain.Game.JumpersModule.toIdsList(gameJumpers);
        return idsEnumerable.Select(gameJumperId =>
        {
            var competitionJumperDto = acl.GetCompetitionJumper(gameJumperId.Item);
            var competitionJumperId = Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id);
            return new Domain.Competition.Jumper(competitionJumperId);
        });
    }
}

public static class HillMapper
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

    public static async Task<Domain.GameWorld.Hill> ToGameWorldHill(this Domain.Competition.Hill hill, IHills hills,
        ICompetitionHillAcl competitionHillAcl, double? overridenMetersByGate = null, CancellationToken ct = default)
    {
        var gameWorldHillDto = competitionHillAcl.GetGameWorldHill(hill.Id.Item);
        var gameWorldHill = await hills.GetById(HillId.NewHillId(gameWorldHillDto.Id), ct)
            .AwaitOrWrap(_ => new IdNotFoundException("GameWorldHill", gameWorldHillDto.Id));
        return gameWorldHill;
    }
}
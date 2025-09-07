using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Domain.Competition;
using App.Domain.Game;
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

    public static IEnumerable<Domain.Simulation.Jumper> ToSimulationJumpers(
        this IEnumerable<Domain.GameWorld.Jumper> gameWorldJumpers)
    {
        return gameWorldJumpers.Select(gameWorldJumper => gameWorldJumper.ToSimulationJumper(likesHill: null));
    }

    public static IEnumerable<GameWorldJumperDto> ToGameWorldJumperDtos(
        this IEnumerable<Domain.Game.JumperId> gameJumperIds, IGameJumperAcl acl)
    {
        return gameJumperIds.Select(gameJumperId =>
        {
            var gameWorldJumperDto = acl.GetGameWorldJumper(gameJumperId.Item);
            return new GameWorldJumperDto(gameWorldJumperDto.Id);
        });
    }

    public static IEnumerable<GameWorldJumperDto> ToGameWorldJumperDtos(
        this IEnumerable<Domain.Game.Jumper> gameJumpers, IGameJumperAcl acl)
    {
        var idsEnumerable = gameJumpers.Select(jumper => jumper.Id);
        return idsEnumerable.Select(gameJumperId =>
        {
            var gameWorldJumperDto = acl.GetGameWorldJumper(gameJumperId.Item);
            return new GameWorldJumperDto(gameWorldJumperDto.Id);
        });
    }

    // public static IEnumerable<GameWorldJumperDto> ToSimulationJumpers(
    //     this IEnumerable<Domain.Competition.Jumper> competitionJumpers, ICompetitionJumperAcl competitionJumperAcl, IGameJumperAcl gameJumperAcl)
    // {
    //     var gameWorldJumpers = competitionJumpers
    //     var idsEnumerable = competitionJumpers.Select(jumper => jumper.Id);
    //     return idsEnumerable.Select(competitionJumperId =>
    //     {
    //         var gameJumperId = competitionJumperAcl.GetGameJumper(competitionJumperId.Item).Id;
    //         var gameWorldJumperDto = gameJumperAcl.GetGameWorldJumper(gameJumperId);
    //         return new GameWorldJumperDto(gameWorldJumperDto.Id);
    //     });
    // }

    public static async Task<IEnumerable<Domain.GameWorld.Jumper>> ToGameWorldJumpers(
        this IEnumerable<Domain.Game.Jumper> gameJumpers, IGameJumperAcl acl, IJumpers jumpers,
        CancellationToken ct = default)
    {
        var gameWorldJumperDtos = gameJumpers.ToGameWorldJumperDtos(acl);
        var gameWorldJumpers = await
            jumpers.GetFromIds(gameWorldJumperDtos.Select(dto => Domain.GameWorld.JumperId.NewJumperId(dto.Id)), ct);
        return gameWorldJumpers;
    }

    public static async Task<IEnumerable<Domain.GameWorld.Jumper>> ToGameWorldJumpers(
        this IEnumerable<Domain.Game.JumperId> gameJumperIds, IGameJumperAcl acl, IJumpers jumpers,
        CancellationToken ct = default)
    {
        var gameWorldJumperDtos = gameJumperIds.ToGameWorldJumperDtos(acl);
        var gameWorldJumpers = await
            jumpers.GetFromIds(gameWorldJumperDtos.Select(dto => Domain.GameWorld.JumperId.NewJumperId(dto.Id)), ct);
        return gameWorldJumpers;
    }

    public static IEnumerable<Domain.Game.Jumper> ToGameJumpers(
        this IEnumerable<Domain.Competition.Jumper> competitionJumpers, ICompetitionJumperAcl acl)
    {
        var idsEnumerable = competitionJumpers.Select(jumper => jumper.Id);
        return idsEnumerable.Select(competitionJumperId =>
        {
            var gameJumperDto = acl.GetGameJumper(competitionJumperId.Item);
            var gameJumperId = Domain.Game.JumperId.NewJumperId(gameJumperDto.Id);
            return new Domain.Game.Jumper(gameJumperId);
        });
    }

    public static IEnumerable<Domain.Competition.Jumper> ToCompetitionJumpers(
        this IEnumerable<Domain.Game.Jumper> gameJumpers, ICompetitionJumperAcl acl)
    {
        var idsEnumerable = gameJumpers.Select(jumper => jumper.Id);
        return idsEnumerable.Select(gameJumperId =>
        {
            var competitionJumperDto = acl.GetCompetitionJumper(gameJumperId.Item);
            var competitionJumperId = Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id);
            return new Domain.Competition.Jumper(competitionJumperId);
        });
    }
    
    public static IEnumerable<Domain.Competition.Jumper> ToCompetitionJumpers(
        this Domain.Game.Jumpers gameJumpers, ICompetitionJumperAcl acl)
    {
        var gameJumperIds = JumpersModule.toIdsList(gameJumpers);
        return gameJumperIds.Select(gameJumperId =>
        {
            var competitionJumperDto = acl.GetCompetitionJumper(gameJumperId.Item);
            var competitionJumperId = Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id);
            return new Domain.Competition.Jumper(competitionJumperId);
        });
    }

    public static IEnumerable<Domain.Competition.Jumper> ToCompetitionJumpers(
        this IEnumerable<Domain.Game.JumperId> gameJumperIds, ICompetitionJumperAcl acl)
    {
        return gameJumperIds.Select(gameJumperId =>
        {
            var competitionJumperDto = acl.GetCompetitionJumper(gameJumperId.Item);
            var competitionJumperId = Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id);
            return new Domain.Competition.Jumper(competitionJumperId);
        });
    }

    public static IEnumerable<Domain.Game.JumperId> ToGameJumperIds(
        this IEnumerable<Domain.Competition.JumperId> competitionJumperIds, ICompetitionJumperAcl acl)
    {
        return competitionJumperIds.Select(competitionJumperId =>
        {
            var gameJumperDto = acl.GetGameJumper(competitionJumperId.Item);
            return Domain.Game.JumperId.NewJumperId(gameJumperDto.Id);
            ;
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
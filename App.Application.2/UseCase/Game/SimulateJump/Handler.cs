using App.Application._2.Acl;
using App.Application._2.Commanding;
using App.Application._2.Exceptions;
using App.Application._2.Extensions;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Messaging.Notifiers.Mapper;
using App.Application._2.Utility;
using App.Domain._2.Competition;
using App.Domain._2.Game;
using App.Domain._2.Simulation;
using Microsoft.FSharp.Collections;
using Gate = App.Domain._2.Simulation.Gate;
using Hill = App.Domain._2.Simulation.Hill;
using HillModule = App.Domain._2.Simulation.HillModule;
using Jumper = App.Domain._2.Simulation.Jumper;
using JumperSkillsModule = App.Domain._2.Simulation.JumperSkillsModule;

namespace App.Application._2.UseCase.Game.SimulateJump;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result(SimulatedJumpDto SimulatedJump);

public class Handler(
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IJumpSimulator jumpSimulator,
    IWeatherEngine weatherEngine,
    IScheduler scheduler,
    IClock clock,
    IGuid guid,
    Acl.ICompetitionHillAcl competitionHillAcl,
    Acl.ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl,
    App.Domain._2.GameWorld.ICountries gameWorldCountries,
    App.Domain._2.GameWorld.IJumpers gameWorldJumpers,
    IMyLogger logger,
    App.Domain._2.GameWorld.IHills hills)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        if (!game.IsDuringCompetition)
        {
            throw new CompetitionNotRunningException(command.GameId);
        }

        var wind = weatherEngine.GetWind();
        var gate = game.CurrentCompetitionGate;
        var nextCompetitionJumper = game.NextCompetitionJumper.Value;
        var competitionHill = game.Hill.Value;

        var gameJumperDto = competitionJumperAcl.GetGameJumper(nextCompetitionJumper.Id.Item);
        var gameWorldJumperDto = gameJumperAcl.GetGameWorldJumper(gameJumperDto.Id);
        logger.Debug($"gameWorldJumperDto: {gameWorldJumperDto}");
        var allGameWorldJumpers = await gameWorldJumpers.GetAll(ct);
        foreach (var jmpr in allGameWorldJumpers)
        {
            logger.Debug($"Jumper in GameWorld: {jmpr.Name} {jmpr.Surname} ({jmpr.Id})");
        }

        var gameWorldJumper =
            await gameWorldJumpers.GetById(Domain._2.GameWorld.JumperId.NewJumperId(gameWorldJumperDto.Id), ct)
                .AwaitOrWrap(_ => new IdNotFoundException(gameWorldJumperDto.Id));

        var jumperSkills = new JumperSkills(
            JumperSkillsModule.BigSkillModule
                .tryCreate(Domain._2.GameWorld.JumperModule.BigSkillModule.value(gameWorldJumper.Takeoff))
                .OrThrow("Wrong takeoff"),
            JumperSkillsModule.BigSkillModule
                .tryCreate(Domain._2.GameWorld.JumperModule.BigSkillModule.value(gameWorldJumper.Flight))
                .OrThrow("Wrong flight"),
            JumperSkillsModule.LandingSkillModule
                .tryCreate(Domain._2.GameWorld.JumperModule.LandingSkillModule.value(gameWorldJumper.Landing))
                .OrThrow($"Wrong landing ({gameWorldJumper.Landing})"),
            JumperSkillsModule.FormModule
                .tryCreate(Domain._2.GameWorld.JumperModule.LiveFormModule.value(gameWorldJumper.LiveForm))
                .OrThrow("Wrong live form"),
            JumperSkillsModule.LikesHillPolicy.None);

        var hill = new Hill(
            HillModule.KPointModule
                .tryCreate(Domain._2.Competition.HillModule.KPointModule.value(competitionHill.KPoint))
                .OrThrow("Wrong kpoint"),
            HillModule.HsPointModule
                .tryCreate(Domain._2.Competition.HillModule.HsPointModule.value(competitionHill.HsPoint))
                .OrThrow("Wrong hs point"),
            new HillSimulationData(HillModule.HsPointModule
                .tryCreate(Domain._2.Competition.HillModule.HsPointModule.value(competitionHill.HsPoint))
                .OrThrow("Wrong hs point")));
        var simulationContext =
            new SimulationContext(Gate.NewGate(App.Domain._2.Competition.GateModule.value(gate)),
                new Jumper(jumperSkills), hill, wind);
        var simulatedJump = jumpSimulator.Simulate(simulationContext);

        var judgeNotes = JumpModule.JudgeNotesModule.tryCreate(ListModule.OfSeq([18.0, 18.5, 18.5, 17.5, 17.5]))
            .OrThrow("Invalid judge notes"); // Komponent Judgement
        var competitionJump = new App.Domain._2.Competition.Jump(nextCompetitionJumper.Id,
            JumpModule.DistanceModule.tryCreate(DistanceModule.value(simulatedJump.Distance))
                .OrThrow("Invalid distance"),
            judgeNotes,
            JumpModule.WindAverage.CreateHeadwind(15.5));

        var jumpResultId = JumpResultId.NewJumpResultId(guid.NewGuid());
        var gameAfterAddingJumpResult = game.AddJumpInCompetition(jumpResultId, competitionJump);
        if (gameAfterAddingJumpResult.IsOk)
        {
            var addJumpOutcome = gameAfterAddingJumpResult.ResultValue;
            var gameAfterAddingJump = addJumpOutcome.Game;
            var changedPhase = addJumpOutcome.PhaseChangedTo;
            if (gameAfterAddingJump.IsDuringCompetition)
            {
                var now = clock.Now();
                await scheduler.ScheduleAsync(
                    jobType: "SimulateJumpInGame",
                    payloadJson: json.Serialize(new { GameId = command.GameId }),
                    runAt: now.AddSeconds(10),
                    uniqueKey: $"SimulateJumpInGame:{command.GameId}_{now.ToUnixTimeSeconds()}",
                    ct: ct);
            }
            else
            {
                if (game.Status.IsPreDraft && ((Domain._2.Game.Status.PreDraft)game.Status).Item.IsBreak)
                {
                    var preDraftStatus = (Domain._2.Game.Status.PreDraft)game.Status;
                    var preDraftBreak = (Domain._2.Game.PreDraftStatus.Break)preDraftStatus.Item;
                    var nextPreDraftCompetitionIndex = PreDraftCompetitionIndexModule.value(preDraftBreak.NextIndex);
                    var now = clock.Now();
                    await scheduler.ScheduleAsync(
                        jobType: "StartNextPreDraftCompetition",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.AddSeconds(15),
                        uniqueKey: $"StartNextPreDraftCompetition:{command.GameId}_{nextPreDraftCompetitionIndex}",
                        ct: ct);
                }
                else if (game.Status.IsBreak && ((Domain._2.Game.Status.Break)game.Status).Next.IsDraftTag)
                {
                    var now = clock.Now();
                    await scheduler.ScheduleAsync(
                        jobType: "StartDraft",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.AddSeconds(25),
                        uniqueKey: $"StartDraft:{command.GameId}",
                        ct: ct);
                }
                else if (game.Status.IsBreak && ((Domain._2.Game.Status.Break)game.Status).Next.IsEndedTag)
                {
                    var now = clock.Now();
                    await scheduler.ScheduleAsync(
                        jobType: "EndGame",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.AddSeconds(20),
                        uniqueKey: $"EndGame:{command.GameId}",
                        ct: ct);
                }
                else
                {
                    throw new InternalCriticalException(
                        $"Competition isn't in progress and no automatic action is defined. Game: {game.Id}, Phase: {
                            game.Status}, Changed Phase: {changedPhase},");
                }
            }

            await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(gameAfterAddingJump));

            var gameWorldCountry = await gameWorldCountries.GetById(gameWorldJumper.CountryId, ct)
                .AwaitOrWrap(_ => new IdNotFoundException(gameWorldJumper.CountryId.Item));

            var jumperResultInClassifiation = gameAfterAddingJump.ClassificationResultOf(nextCompetitionJumper.Id)
                .OrThrow($"Missing classification result for competition jumper {nextCompetitionJumper.Id.Item}");

            var simulatedJumpDto = new SimulatedJumpDto(nextCompetitionJumper.Id.Item, gameWorldJumperDto.Id,
                gameWorldJumper.Name.Item, gameWorldJumper.Surname.Item,
                Domain._2.GameWorld.FisCodeModule.value(gameWorldCountry.FisCode),
                DistanceModule.value(simulatedJump.Distance), JumpModule.JudgeNotesModule.value(judgeNotes).ToArray(),
                WindModule.averaged(wind),
                Domain._2.Competition.TotalPointsModule.value(jumperResultInClassifiation.Points),
                Domain._2.Competition.Classification.PositionModule.value(jumperResultInClassifiation.Position));

            return new Result(simulatedJumpDto);
        }

        throw new Exception("Error adding a jump to game.");
    }
}

public class InternalCriticalException(string? message = null) : Exception(message);

public class CompetitionNotRunningException(Guid gameId, string? message = null) : Exception(message)
{
    public Guid GameId { get; } = gameId;
}

public record SimulatedJumpDto(
    Guid CompetitionJumperId,
    Guid GameWorldJumperId,
    string Name,
    string Surname,
    string CountryFisCode,
    double Distance,
    double[] JudgeNotes,
    double WindAverage,
    double TotalPoints,
    int CurrentRank);
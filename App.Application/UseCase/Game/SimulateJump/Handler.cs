using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.GameCompetitions;
using App.Application.Game.GameSimulationPack;
using App.Application.Game.GameWind;
using App.Application.JumpersForm;
using App.Application.Mapping;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using App.Domain.Simulation;
using Microsoft.FSharp.Collections;
using Gate = App.Domain.Simulation.Gate;
using Hill = App.Domain.Simulation.Hill;
using HillModule = App.Domain.Simulation.HillModule;
using Jumper = App.Domain.Simulation.Jumper;
using JumperId = App.Domain.Competition.JumperId;
using JumperSkillsModule = App.Domain.Simulation.JumperSkillsModule;

namespace App.Application.UseCase.Game.SimulateJump;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result(SimulatedJumpDto SimulatedJump);

public class Handler(
    IJson json,
    IGames games,
    IGameNotifier gameNotifier,
    IGameSimulationPack gameSimulationPack,
    IScheduler scheduler,
    IClock clock,
    IGuid guid,
    Acl.ICompetitionJumperAcl competitionJumperAcl,
    IGameJumperAcl gameJumperAcl,
    App.Domain.GameWorld.IJumpers gameWorldJumpers,
    IMyLogger logger,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IJumperGameFormStorage jumperGameFormStorage,
    IGameCompetitionResultsArchive competitionResultsArchive,
    IGameSchedule gameSchedule,
    IGameWind gameWind)
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

        var simulationPack = gameSimulationPack.GetFor(command.GameId);

        var simulationWind = simulationPack.WeatherEngine.GetWind();
        var gate = game.CurrentCompetitionGate;
        var gateInt = App.Domain.Competition.GateModule.value(gate);
        var nextCompetitionJumper = game.NextCompetitionJumper.Value;
        var competitionHill = game.Hill.Value;

        var gameJumperDto = competitionJumperAcl.GetGameJumper(nextCompetitionJumper.Id.Item);
        var gameWorldJumperDto = gameJumperAcl.GetGameWorldJumper(gameJumperDto.Id);

        logger.Debug($"$Now jumps GameWorld Jumper with ID {gameWorldJumperDto.Id}. (GameJumper ID: {gameJumperDto.Id
        }, CompetitionJumper ID: {nextCompetitionJumper.Id}");

        var gameWorldJumper =
            await gameWorldJumpers.GetById(Domain.GameWorld.JumperId.NewJumperId(gameWorldJumperDto.Id), ct)
                .AwaitOrWrap(_ =>
                {
                    logger.Error($"GameWorld Jumper with ID {gameWorldJumperDto.Id} not found.");
                    return new IdNotFoundException(gameWorldJumperDto.Id);
                });

        var gameForm = jumperGameFormStorage.GetGameForm(gameJumperDto.Id);
        var simulationJumper = gameWorldJumper.ToSimulationJumper(likesHill: null, form: gameForm);
        var simulationHill = competitionHill.ToSimulationHill(overridenMetersByGate: null);

        var simulationContext =
            new SimulationContext(Gate.NewGate(gateInt),
                simulationJumper, simulationHill, simulationWind);
        var simulatedJump = simulationPack.JumpSimulator.Simulate(simulationContext);

        logger.Info($"{gameWorldJumper.Name.Item} {gameWorldJumper.Surname.Item} jumped: {
            DistanceModule.value(simulatedJump.Distance)}m + {simulatedJump.Landing} ({
                WindModule.average(simulationWind):F2}m/s)");

        var judgesSimulationContext =
            new JudgesSimulationContext(simulatedJump, Gate.NewGate(App.Domain.Competition.GateModule.value(gate)),
                simulationJumper, simulationHill, simulationWind);
        var simulatedJudges = simulationPack.JudgesSimulator.Evaluate(judgesSimulationContext);

        var competitionJudges = JumpModule.JudgesModule
            .tryCreate(JudgesModule.value(simulatedJudges))
            .OrThrow("Invalid judge notes");

        var competitionJumpWind =
            App.Domain.Competition.JumpModule.WindAverage.FromDouble(WindModule.average(simulationWind));
        var competitionJump = new App.Domain.Competition.Jump(nextCompetitionJumper.Id,
            JumpModule.DistanceModule.tryCreate(DistanceModule.value(simulatedJump.Distance))
                .OrThrow("Invalid distance"),
            competitionJudges,
            competitionJumpWind,
            Domain.Competition.Gate.NewGate(gateInt)); // TODO: Nie powinniśmy ustawiać tego w warstwie Application

        var jumpResultId = JumpResultId.NewJumpResultId(guid.NewGuid());
        var gameAfterAddingJumpResult = game.AddJumpInCompetition(jumpResultId, competitionJump);
        if (gameAfterAddingJumpResult.IsOk)
        {
            var addJumpOutcome = gameAfterAddingJumpResult.ResultValue;
            var gameAfterAddingJump = addJumpOutcome.Game;
            var competitionAfterAddingJump = addJumpOutcome.Competition;
            var changedPhase = addJumpOutcome.PhaseChangedTo;

            await games.Add(gameAfterAddingJump, ct);

            var classificationResult =
                competitionAfterAddingJump.ClassificationResultOf(nextCompetitionJumper.Id).Value;
            logger.Debug($"{gameWorldJumper.Name.Item} {gameWorldJumper.Surname.Item}: Pos. {
                Domain.Competition.Classification.PositionModule.value(classificationResult.Position)}({
                    classificationResult.Points.Item
                }pts)");

            var previousCompetition = addJumpOutcome.Competition;

            if (gameAfterAddingJump.IsDuringCompetition)
            {
                var timeToJump = gameAfterAddingJump.Settings.CompetitionJumpInterval.Value;
                var now = clock.Now();
                gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.CompetitionJump, timeToJump);
                await scheduler.ScheduleAsync(
                    jobType: "SimulateJumpInGame",
                    payloadJson: json.Serialize(new { GameId = command.GameId }),
                    runAt: now.Add(timeToJump),
                    uniqueKey: $"SimulateJumpInGame:{command.GameId}_{now.ToUnixTimeSeconds()}",
                    ct: ct);
            }
            else
            {
                logger.Debug(
                    $"Competition (Game ID: {command.GameId}) CurrentCompetitionClassification: " +
                    string.Join(", ",
                        competitionAfterAddingJump.Classification
                            .Select(result => $"{result.JumperId.Item} {result.Points.Item}"))
                );

                if (gameAfterAddingJump.Status.IsPreDraft &&
                    ((Domain.Game.Status.PreDraft)gameAfterAddingJump.Status).Item.IsBreak)
                {
                    await ArchivePreDraftCompetitionResults(addJumpOutcome, command.GameId, ct);
                    var preDraftStatus = (Domain.Game.Status.PreDraft)gameAfterAddingJump.Status;
                    var preDraftBreak = (Domain.Game.PreDraftStatus.Break)preDraftStatus.Item;
                    var nextPreDraftCompetitionIndex = PreDraftCompetitionIndexModule.value(preDraftBreak.NextIndex);
                    var timeToNextPreDraftCompetition = gameAfterAddingJump.Settings.BreakSettings
                        .BreakBetweenPreDraftCompetitions.Value;
                    var now = clock.Now();
                    gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.PreDraftNextCompetition,
                        timeToNextPreDraftCompetition);
                    await scheduler.ScheduleAsync(
                        jobType: "StartNextPreDraftCompetition",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.Add(timeToNextPreDraftCompetition),
                        uniqueKey: $"StartNextPreDraftCompetition:{command.GameId}_{nextPreDraftCompetitionIndex}",
                        ct: ct);
                }
                else if (gameAfterAddingJump.Status.IsBreak &&
                         ((Domain.Game.Status.Break)gameAfterAddingJump.Status).Next.IsDraftTag)
                {
                    await ArchivePreDraftCompetitionResults(addJumpOutcome, command.GameId, ct);
                    previousCompetition = addJumpOutcome.Competition;
                    var timeToDraft = gameAfterAddingJump.Settings.BreakSettings
                        .BreakBeforeDraft.Value;
                    gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.Draft,
                        timeToDraft);
                    var now = clock.Now();
                    await scheduler.ScheduleAsync(
                        jobType: "StartDraft",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.Add(timeToDraft),
                        uniqueKey: $"StartDraft:{command.GameId}",
                        ct: ct);
                }
                else if (gameAfterAddingJump.Status.IsBreak &&
                         ((Domain.Game.Status.Break)gameAfterAddingJump.Status).Next.IsEndedTag)
                {
                    await ArchiveMainCompetitionResults(addJumpOutcome, command.GameId, ct);
                    previousCompetition = addJumpOutcome.Competition;
                    var timeToEnd = gameAfterAddingJump.Settings.BreakSettings
                        .BreakBeforeEnd.Value;
                    gameSchedule.ScheduleEvent(command.GameId, GameScheduleTarget.End, timeToEnd);
                    var now = clock.Now();
                    await scheduler.ScheduleAsync(
                        jobType: "EndGame",
                        payloadJson: json.Serialize(new { GameId = command.GameId }),
                        runAt: now.Add(timeToEnd),
                        uniqueKey: $"EndGame:{command.GameId}",
                        ct: ct);
                }
                else
                {
                    throw new InternalCriticalException(
                        $"Competition isn't in progress and no automatic action is defined. Game: {
                            gameAfterAddingJump.Id}, Phase: {
                                gameAfterAddingJump.Status}, Changed Phase: {changedPhase},");
                }
            }

            logger.Info("Previous competition: " + (previousCompetition is null
                ? "null"
                : $"not null, next jumper exists: {previousCompetition.NextJumper.IsSome()}"));

            simulationPack.WeatherEngine.SimulateTime(TimeSpan.FromMinutes(1));
            var generatedWind = simulationPack.WeatherEngine.GetWind();
            var generatedWindDouble = WindModule.average(generatedWind);
            gameWind.Set(command.GameId, generatedWind);
            var toBeatDistance = competitionAfterAddingJump.ToBeatDistance(generatedWindDouble);

            var gameUpdatedDto = await CreateGameUpdatedDto(gameAfterAddingJump, toBeatDistance.ToNullable(),
                previousCompetition,
                nextCompetitionJumper.Id, ct);
            await gameNotifier.GameUpdated(gameUpdatedDto
            );

            var jumperResultInClassifiation = addJumpOutcome.Competition
                .ClassificationResultOf(nextCompetitionJumper.Id)
                .OrThrow($"Missing classification result for competition jumper {nextCompetitionJumper.Id.Item}");

            var simulatedJumpDto = new SimulatedJumpDto(nextCompetitionJumper.Id.Item, gameWorldJumperDto.Id,
                gameWorldJumper.Name.Item, gameWorldJumper.Surname.Item,
                Domain.GameWorld.CountryFisCodeModule.value(gameWorldJumper.FisCountryCode),
                DistanceModule.value(simulatedJump.Distance),
                JumpModule.JudgesModule.value(competitionJudges).ToArray(),
                WindModule.average(simulationWind),
                Domain.Competition.TotalPointsModule.value(jumperResultInClassifiation.Points),
                Domain.Competition.Classification.PositionModule.value(jumperResultInClassifiation.Position));

            return new Result(simulatedJumpDto);
        }

        throw new Exception("Error adding a jump to game: " + gameAfterAddingJumpResult.ErrorValue);
    }


    private async Task ArchivePreDraftCompetitionResults(AddJumpOutcome addJumpOutcome, Guid gameId,
        CancellationToken ct)
    {
        var archiveDto = addJumpOutcome.Classification.ToGameCompetitionResultsArchiveDto(gameJumperAcl,
            competitionJumperAcl, competitionJumperId =>
                GetCompetitionJumperJumperBibOrThrow(addJumpOutcome, competitionJumperId));
        await competitionResultsArchive.ArchivePreDraftAsync(gameId,
            archiveDto, ct);
    }

    private async Task<GameUpdatedDto> CreateGameUpdatedDto(App.Domain.Game.Game game, double? toBeat,
        App.Domain.Competition.Competition? lastCompetitionState,
        App.Domain.Competition.JumperId lastCompetitionJumperId, CancellationToken ct)
    {
        var dto = await gameUpdatedDtoMapper.FromDomain(game, toBeat, lastDraftState: null,
            lastCompetitionState: lastCompetitionState,
            lastCompetitionJumperId.Item, ct: ct);
        return dto;
    }

    private async Task ArchiveMainCompetitionResults(AddJumpOutcome addJumpOutcome, Guid gameId, CancellationToken ct)
    {
        var archiveDto = addJumpOutcome.Classification.ToGameCompetitionResultsArchiveDto(gameJumperAcl,
            competitionJumperAcl, competitionJumperId =>
                GetCompetitionJumperJumperBibOrThrow(addJumpOutcome, competitionJumperId));
        await competitionResultsArchive.ArchiveMainAsync(gameId,
            archiveDto, ct);
    }

    private static int GetCompetitionJumperJumperBibOrThrow(AddJumpOutcome addJumpOutcome, Guid competitionJumperId)
    {
        var bibOption =
            addJumpOutcome.Competition.Startlist_.BibOf(JumperId.NewJumperId(competitionJumperId));
        if (bibOption.IsNone())
        {
            throw new Exception($"Missing bib for jumper {competitionJumperId}.");
        }

        return StartlistModule.BibModule.value(bibOption.Value);
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
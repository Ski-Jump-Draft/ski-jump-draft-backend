using System.Collections.Immutable;
using System.Text.Json;
using App.Application.Acl;
using App.Application.Bot;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Mapping;
using App.Application.Service;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using App.Domain.GameWorld;
using Jumper = App.Domain.Competition.Jumper;
using JumperId = App.Domain.Game.JumperId;

namespace App.Application.Messaging.Notifiers.Mapper;

using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

public class GameUpdatedDtoMapper(
    Func<Domain.Competition.JumperId, CancellationToken, Task<Domain.GameWorld.Jumper>>
        gameWorldJumperByCompetitionJumperId,
    PreDraftPositionsService preDraftPositionsService,
    IGameSchedule gameSchedule,
    IMyLogger logger,
    IClock clock,
    IBotRegistry botRegistry,
    IGameJumperAcl gameJumperAcl,
    IJumpers gameWorldJumpers,
    ICompetitionJumperAcl competitionJumperAcl)
{
    private const int SchemaVersion = 1;

    // ---------- Helpers: unwrap F# single-case VOs ----------
    private static Guid Unwrap(GameId x) => x.Item;
    private static Guid Unwrap(PlayerId x) => x.Item;
    private static Guid Unwrap(App.Domain.Game.JumperId x) => x.Item;
    private static Guid Unwrap(App.Domain.Competition.JumperId x) => x.Item;
    private static Guid Unwrap(App.Domain.Competition.HillId x) => x.Item;

    private static (string Case, object[] Fields) DeconstructUnion(object union)
    {
        var t = union.GetType();
        var (uci, fields) = (
            FSharpValue.GetUnionFields(union, t, null).Item1,
            FSharpValue.GetUnionFields(union, t, null).Item2
        );
        return (uci.Name, fields);
    }


    public async Task<GameUpdatedDto> FromDomain(App.Domain.Game.Game game,
        App.Domain.Competition.Competition? lastCompetitionState = null, Guid? lastCompetitionJumperId = null,
        string changeType = "Snapshot", CancellationToken ct = default)
    {
        var header = await MapHeader(game, ct);
        var nextStatus = MapNextStatus(game.Id.Item);
        var (statusStr, preDraft, draft, mainComp, brk, ended) = await MapStatus(game, ct);

        CompetitionRoundResultDto? lastCompetitionResultDto = null;
        if (lastCompetitionJumperId is not null)
        {
            if (lastCompetitionState is null && !game.IsDuringCompetition)
            {
                throw new ArgumentNullException($"(Game {game.Id.Item
                }) lastCompetitionState is null, but game is not during competition.");
            }

            var classification = game.IsDuringCompetition
                ? game.CurrentCompetitionClassification
                : lastCompetitionState!.Classification;
            var lastClassificationResult =
                classification.Single(result =>
                    result.JumperId.Item == lastCompetitionJumperId!);
            var lastJumpResult = lastClassificationResult.JumpResults.Last();
            lastCompetitionResultDto = CreateCompetitionRoundResultDto(lastJumpResult);
        }

        var nextStatusStr = nextStatus != null ? $"{nextStatus.Status} IN {nextStatus.In.TotalSeconds}" : null;
        logger.Info($"(Game {game.Id.Item}) next status: {nextStatusStr ?? "NONE"}");

        return new GameUpdatedDto(
            Unwrap(game.Id_),
            SchemaVersion,
            statusStr,
            nextStatus,
            changeType,
            game.Settings.PreDraftSettings.CompetitionsCount,
            header,
            preDraft,
            draft,
            mainComp,
            brk,
            ended,
            lastCompetitionState != null ? await MapCompetition(lastCompetitionState, game.Id.Item) : null,
            lastCompetitionResultDto
        );
    }

    // ────────────────────────────── Header ──────────────────────────────
    private async Task<GameHeaderDto> MapHeader(App.Domain.Game.Game game, CancellationToken ct)
    {
        var gameId = game.Id.Item;

        var gamePlayers = PlayersModule.toList(game.Players)
            .Select(player =>
            {
                var playerId = player.Id.Item;
                var isBot = botRegistry.IsGameBot(gameId, playerId);
                return new GamePlayerDto(Unwrap(player.Id), PlayerModule.NickModule.value(player.Nick), isBot);
            })
            .ToList();

        var gameJumpers = JumpersModule.toList(game.Jumpers);
        var gameWorldJumpersList = await gameJumpers.ToGameWorldJumpers(gameJumperAcl, gameWorldJumpers, ct);

        var gameJumperDtos = new List<GameJumperDto>();
        var competitionJumperDtos = new List<CompetitionJumperDto>();
        foreach (var gameWorldJumper in gameWorldJumpersList)
        {
            var gameJumperId = gameJumperAcl.GetGameJumper(gameWorldJumper.Id.Item).Id;
            var competitionJumperId = competitionJumperAcl.GetCompetitionJumper(gameJumperId).Id;
            var name = gameWorldJumper.Name.Item;
            var surname = gameWorldJumper.Surname.Item;
            var fisCountryCode = CountryFisCodeModule.value(gameWorldJumper.FisCountryCode);
            gameJumperDtos.Add(new GameJumperDto(gameJumperId, gameWorldJumper.Id.Item, name,
                surname,
                fisCountryCode));
            competitionJumperDtos.Add(new CompetitionJumperDto(gameJumperId, competitionJumperId, name, surname,
                fisCountryCode));
        }

        var competitionHillId = game.Hill.Match(
            some: hill => Unwrap(hill.Id),
            none: () => (Guid?)null
        );

        return new GameHeaderDto(competitionHillId, gamePlayers, gameJumperDtos, competitionJumperDtos);
    }

    private NextStatusDto? MapNextStatus(Guid gameId)
    {
        var schedule = gameSchedule.GetGameSchedule(gameId);
        if (schedule is null) return null;
        if (schedule.BreakPassed(clock)) return null;
        if (schedule.ScheduleTarget == GameScheduleTarget.CompetitionJump) return null;
        var nextStatusDto = new NextStatusDto(schedule.ScheduleTarget.ToString(), schedule.In);
        return nextStatusDto;
    }

    private async Task<(string, PreDraftDto?, DraftDto?, CompetitionDto?, BreakDto?, EndedDto?)> MapStatus(
        App.Domain.Game.Game game, CancellationToken ct)
    {
        var (caseName, fields) = DeconstructUnion(game.Status);

        return caseName switch
        {
            "PreDraft" => ("PreDraft", await MapPreDraft((App.Domain.Game.PreDraftStatus)fields[0], game.Id.Item), null,
                null, null,
                null),
            "Draft" => ("Draft", null, await MapDraft(game, (App.Domain.Game.Draft)fields[0], ct), null, null, null),
            "MainCompetition" => ("MainCompetition", null, null,
                await MapCompetition((Competition)fields[0], game.Id.Item), null,
                null),
            "Ended" => ("Ended", null, null, null, null, MapEnded(game)),
            "Break" => ($"Break {fields[0].ToString()!.Replace("Tag", "")}", null, null, null,
                MapBreak((App.Domain.Game.StatusTag)fields[0]), null),
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{caseName}'.")
        };
    }

    // ---------- PreDraft ----------
    private async Task<PreDraftDto> MapPreDraft(App.Domain.Game.PreDraftStatus pre, Guid gameId)
    {
        var (caseName, fields) = DeconstructUnion(pre);

        return caseName switch
        {
            "Running" => new PreDraftDto(
                "Running",
                PreDraftCompetitionIndexModule.value((App.Domain.Game.PreDraftCompetitionIndex)fields[0]),
                await MapCompetition((Competition)fields[1], gameId)),
            "Break" => new PreDraftDto(
                "Break",
                PreDraftCompetitionIndexModule.value((App.Domain.Game.PreDraftCompetitionIndex)fields[0]),
                null),
            _ => throw new InvalidOperationException($"Unknown PreDraftStatus case '{caseName}'.")
        };
    }

    // ---------- Draft ----------
    private async Task<DraftDto> MapDraft(App.Domain.Game.Game game, App.Domain.Game.Draft draft, CancellationToken ct)
    {
        var currentPlayerId = draft.CurrentTurn.Match(
            some: turn => Unwrap(turn.PlayerId),
            none: () => (Guid?)null
        );

        var picks = PlayersModule.toList(game.Players)
            .Select(pl =>
            {
                var picked = draft.PicksOf(pl.Id)
                    .Match(
                        some: lst => lst.Select(Unwrap).ToList(),
                        none: () => new List<Guid>()
                    );
                return new PlayerPicksDto(Unwrap(pl.Id), picked);
            })
            .ToList()
            .AsReadOnly();

        var positionsByGameJumper = preDraftPositionsService.GetPreDraftPositions(game.Id.Item).PositionsByGameJumper;

        var tasks = draft.AvailablePicks.Select(async gameJumperId =>
        {
            var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(gameJumperId.Item).Id;
            var gameWorldJumper = await gameWorldJumpers
                .GetById(Domain.GameWorld.JumperId.NewJumperId(gameWorldJumperId), ct)
                .AwaitOrWrap(_ => new IdNotFoundException(gameWorldJumperId));

            return new DraftPickOptionDto(
                gameJumperId.Item,
                gameWorldJumper.Name.Item,
                gameWorldJumper.Surname.Item,
                CountryFisCodeModule.value(gameWorldJumper.FisCountryCode),
                positionsByGameJumper[gameJumperId.Item]
            );
        });

        var availableJumpers = await Task.WhenAll(tasks);

        int? timeoutInSeconds = game.Settings.DraftSettings.TimeoutPolicy switch
        {
            DraftModule.SettingsModule.TimeoutPolicy.TimeoutAfter timeoutAfter => timeoutAfter.Time.Seconds,
            var timeoutPolicy when timeoutPolicy.IsNoTimeout => null,
            _ => throw new InvalidOperationException($"Unknown DraftSettings.TimeoutPolicy: {
                game.Settings.DraftSettings.TimeoutPolicy}")
        };
        var nextPlayers = draft.TurnQueueRemaining.Select(id => id.Item);
        var orderPolicyString = game.Settings.DraftSettings.Order switch
        {
            var v when v.IsClassic => "Classic",
            var v when v.IsSnake => "Snake",
            var v when v.IsRandom => "Random",
            _ => throw new InvalidOperationException(
                $"Unknown DraftSettings.Order: {game.Settings.DraftSettings.Order}")
        };
        return new DraftDto(currentPlayerId, timeoutInSeconds, draft.Ended, orderPolicyString, picks,
            availableJumpers.ToList().AsReadOnly(), nextPlayers.ToList());
    }


    private async Task<CompetitionDto> MapCompetition(Competition comp, Guid gameId)
    {
        var status = comp.GetStatusTag switch
        {
            var v when v.Equals(CompetitionModule.StatusTag.NotStartedTag) => "NotStarted",
            var v when v.Equals(CompetitionModule.StatusTag.RoundInProgressTag) => "RoundInProgress",
            var v when v.Equals(CompetitionModule.StatusTag.SuspendedTag) => "Suspended",
            var v when v.Equals(CompetitionModule.StatusTag.CancelledTag) => "Cancelled",
            var v when v.Equals(CompetitionModule.StatusTag.EndedTag) => "Ended",
            _ => "Unknown"
        };

        var nextJumperId =
            comp.NextJumper.Match(
                some: j => Unwrap(j.Id),
                none: () => (Guid?)null
            );

        var gate = MapGate(comp.GateState);
        var results = await MapResults(comp.Jumpers_, comp.Classification, comp.Startlist_);
        var schedule = gameSchedule.GetGameSchedule(gameId);
        int? nextJumpIn = schedule switch
        {
            { ScheduleTarget: GameScheduleTarget.CompetitionJump } => (int)Math.Ceiling(schedule.In
                .TotalSeconds),
            _ => null
        };
        var endedEntries = comp.Startlist_.DoneEntries.Select(entry =>
            new StartlistJumperDto(StartlistModule.BibModule.value(entry.Bib), true, entry.JumperId.Item));
        var notEndedEntries = comp.Startlist_.RemainingEntries.Select(entry =>
            new StartlistJumperDto(StartlistModule.BibModule.value(entry.Bib), false, entry.JumperId.Item));

        IReadOnlyList<StartlistJumperDto> startlist =
        [
            ..endedEntries, ..notEndedEntries
        ];

        return new CompetitionDto(status, startlist, gate,
            results.ToImmutableList(), nextJumpIn);
    }

    private static GateDto MapGate(FSharpOption<GateState> gsOpt)
    {
        if (!FSharpOption<GateState>.get_IsSome(gsOpt))
            return new GateDto(0, 0, null);

        var gs = gsOpt.Value;

        var starting = GateModule.value(gs.Starting);
        var current = GateModule.value(gs.CurrentJury);

        var coachReduction = gs.CoachChange.Match(
            some: ch =>
            {
                var (chCase, chFields) = DeconstructUnion(ch);
                return chCase switch
                {
                    "Reduction" => (int?)(int)(uint)chFields[0],
                    _ => null
                };
            },
            none: () => (int?)null
        );

        return new GateDto(starting, current, coachReduction);
    }

    private async Task<IEnumerable<CompetitionResultDto>> MapResults(
        IEnumerable<Jumper> competitionJumpers,
        IEnumerable<Classification.JumperClassificationResult> classificationResults,
        Startlist startlist,
        CancellationToken ct = default)
    {
        var tasks = classificationResults.Select(async classificationResult =>
        {
            var competitionJumperId = Unwrap(classificationResult.JumperId);
            var jumperDomainId = Domain.Competition.JumperId.NewJumperId(competitionJumperId);
            var totalPoints = TotalPointsModule.value(classificationResult.Points);

            var rank = Classification.PositionModule.value(classificationResult.Position);

            var bib = startlist.BibOf(jumperDomainId);
            if (bib.IsNone())
                throw new InvalidOperationException($"Missing bib for jumper {competitionJumperId}.");
            var bibValue = StartlistModule.BibModule.value(bib.Value);

            var competitionJumper = competitionJumpers.SingleOrDefault(jumper => jumper.Id.Equals(jumperDomainId));
            if (competitionJumper is null)
                throw new InvalidOperationException($"Missing jumper {competitionJumperId} in competition.");

            var jumpResultDtos = classificationResult.JumpResults.Select(CreateCompetitionRoundResultDto);

            return new CompetitionResultDto(
                rank, bibValue, competitionJumperId, totalPoints,
                jumpResultDtos.ToImmutableList()
            );
        });

        return
            await Task.WhenAll(
                tasks);
    }

    private CompetitionRoundResultDto CreateCompetitionRoundResultDto(JumpResult jumpResult)
    {
        double? judgePoints = jumpResult.JudgePoints.IsSome()
            ? JumpResultModule.JudgePointsModule.value(jumpResult.JudgePoints.Value)
            : null;
        double? windPoints = jumpResult.WindPoints.IsSome()
            ? JumpResultModule.WindPointsModule.value(jumpResult.WindPoints.Value)
            : null;
        double? gatePoints = jumpResult.GatePoints.IsSome()
            ? JumpResultModule.GatePointsModule.value(jumpResult.GatePoints.Value)
            : null;
        var totalCompetitionPoints = JumpResultModule.TotalCompensationModule.value(jumpResult.TotalCompensation);
        var windAverage = jumpResult.Jump.Wind.ToDouble();
        return new CompetitionRoundResultDto(
            competitionJumperAcl.GetGameJumper(jumpResult.JumperId.Item).Id,
            jumpResult.JumperId.Item,
            JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
            jumpResult.TotalPoints.Item,
            JumpModule.JudgesModule.value(jumpResult.Jump.JudgeNotes),
            judgePoints,
            windPoints,
            windAverage,
            gatePoints, totalCompetitionPoints
        );
    }


// ---------- Break / Ended ----------
    private static BreakDto MapBreak(App.Domain.Game.StatusTag nextTag)
    {
        var next = nextTag switch
        {
            { IsPreDraftTag: true } => "PreDraft",
            { IsDraftTag: true } => "Draft",
            { IsMainCompetitionTag: true } => "MainCompetition",
            { IsEndedTag: true } => "Ended",
            _ => "Unknown"
        };

        return new BreakDto(next);
    }

    private static EndedDto MapEnded(App.Domain.Game.Game game)
    {
        var policy = game.Settings.RankingPolicy switch
        {
            var v when v.Equals(Domain.Game.RankingPolicy.Classic) => "Classic",
            var v when v.Equals(Domain.Game.RankingPolicy.PodiumAtAllCosts) => "PodiumAtAllCosts",
            _ => "Classic"
        };

        var ranking = ((Domain.Game.Status.Ended)game.Status).Item;
        var positionAndPoints = ranking.PositionsAndPoints;

        var positionAndPointsMap = positionAndPoints
            .ToDictionary(
                kvp => kvp.Key.Item,
                kvp => (RankingModule.PositionModule.value(kvp.Value.Item1),
                    RankingModule.PointsModule.value(kvp.Value.Item2))
            );

        return new EndedDto(policy, positionAndPointsMap);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Little Option helpers (ergonomic Match over F# Option)
// ─────────────────────────────────────────────────────────────────────────────
internal static class FSharpOptionExt
{
    public static TOut Match<T, TOut>(
        this FSharpOption<T> opt,
        Func<T, TOut>
            some,
        Func<TOut> none)
        => FSharpOption<T>.get_IsSome(opt) ? some(opt.Value) : none();
}
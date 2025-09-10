using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.GameCompetitions;
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
    // Func<Domain.Game.JumperId, CancellationToken, Task<Domain.GameWorld.Jumper>>
    //     gameWorldJumperByGameJumperId,
    IGameJumperAcl gameJumperAcl,
    ICompetitionJumperAcl competitionJumperAcl,
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    IJumpers gameWorldJumpers)
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
        App.Domain.Competition.Competition? lastCompetitionState = null,
        string changeType = "Snapshot", CancellationToken ct = default)
    {
        var header = MapHeader(game);
        var (statusStr, preDraft, draft, mainComp, brk, ended) = await MapStatus(game, ct);

        return new GameUpdatedDto(
            Unwrap(game.Id_),
            SchemaVersion,
            statusStr,
            changeType,
            game.Settings.PreDraftSettings.CompetitionsCount,
            header,
            preDraft,
            draft,
            mainComp,
            brk,
            ended,
            lastCompetitionState != null ? await MapCompetition(lastCompetitionState) : null
        );
    }

    // ────────────────────────────── Header ──────────────────────────────
    private GameHeaderDto MapHeader(App.Domain.Game.Game game)
    {
        var players = PlayersModule.toList(game.Players)
            .Select(p => new PlayerDto(Unwrap(p.Id), PlayerModule.NickModule.value(p.Nick)))
            .ToList();

        var jumpers = JumpersModule.toList(game.Jumpers)
            .Select(j => new JumperDto(Unwrap(j.Id)))
            .ToList();

        var hillId = game.Hill.Match(
            some: hill => Unwrap(hill.Id),
            none: () => (Guid?)null
        );

        return new GameHeaderDto(hillId, players, jumpers);
    }

    private async Task<(string, PreDraftDto?, DraftDto?, CompetitionDto?, BreakDto?, EndedDto?)> MapStatus(
        App.Domain.Game.Game game, CancellationToken ct)
    {
        var (caseName, fields) = DeconstructUnion(game.Status);

        return caseName switch
        {
            "PreDraft" => ("PreDraft", await MapPreDraft((App.Domain.Game.PreDraftStatus)fields[0]), null, null, null,
                null),
            "Draft" => ("Draft", null, await MapDraft(game, (App.Domain.Game.Draft)fields[0], ct), null, null, null),
            "MainCompetition" => ("MainCompetition", null, null, await MapCompetition((Competition)fields[0]), null,
                null),
            "Ended" => ("Ended", null, null, null, null, MapEnded(game)),
            "Break" => ($"Break {fields[0].ToString()!.Replace("Tag", "")}", null, null, null,
                MapBreak((App.Domain.Game.StatusTag)fields[0]), null),
            _ => throw new InvalidOperationException($"Unknown Game.Status case '{caseName}'.")
        };
    }

    // ---------- PreDraft ----------
    private async Task<PreDraftDto> MapPreDraft(App.Domain.Game.PreDraftStatus pre)
    {
        var (caseName, fields) = DeconstructUnion(pre);

        return caseName switch
        {
            "Running" => new PreDraftDto(
                "Running",
                PreDraftCompetitionIndexModule.value((App.Domain.Game.PreDraftCompetitionIndex)fields[0]),
                await MapCompetition((Competition)fields[1])),
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

        var positionsByGameJumper = new Dictionary<Guid, List<int>>();
        var preDraftResults = gameCompetitionResultsArchive.GetPreDraftResults(game.Id.Item)
                              ?? throw new InvalidOperationException(
                                  $"Game {game.Id} does not have pre-draft results in archive.");

        foreach (var preDraftCompetitionResults in preDraftResults)
        {
            foreach (var (competitionJumperId, gameJumperPosition, _) in preDraftCompetitionResults.Results)
            {
                var gameJumperId = competitionJumperAcl.GetGameJumper(competitionJumperId).Id;
                if (!positionsByGameJumper.TryGetValue(gameJumperId, out var positionsList))
                    positionsByGameJumper.Add(gameJumperId, [gameJumperPosition]);
                else
                    positionsList.Add(gameJumperPosition);
            }
        }

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


    private async Task<CompetitionDto> MapCompetition(Competition comp)
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

        Guid? nextJumperId =
            comp.NextJumper.Match(
                some: j => Unwrap(j.Id),
                none: () => (Guid?)null
            );

        var gate = MapGate(comp.GateState);
        var results = await MapResults(comp.Jumpers_, comp.Classification, comp.Startlist_);

        return new CompetitionDto(status, nextJumperId, gate, results.ToImmutableList());
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

            var gameWorldJumper = await gameWorldJumperByCompetitionJumperId(competitionJumper.Id, ct);

            var competitionJumperDto = new CompetitionJumperDto(
                competitionJumperId,
                gameWorldJumper.Name.Item,
                gameWorldJumper.Surname.Item,
                CountryFisCodeModule.value(gameWorldJumper.FisCountryCode)
            );

            var jumpResultDtos = classificationResult.JumpResults.Select(jumpResult =>
            {
                double? judgePoints = jumpResult.JudgePoints.IsSome()
                    ? JumpResultModule.JudgePointsModule.value(jumpResult.JudgePoints.Value)
                    : null;
                double? windPoints = jumpResult.WindPoints.IsSome()
                    ? JumpResultModule.WindPointsModule.value(jumpResult.WindPoints.Value)
                    : null;
                var windAverage = jumpResult.Jump.Wind.ToDouble();
                return new CompetitionRoundResultDto(
                    JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
                    jumpResult.TotalPoints.Item,
                    judgePoints,
                    windPoints,
                    windAverage
                );
            });

            return new CompetitionResultDto(
                rank, bibValue, competitionJumperDto, totalPoints,
                jumpResultDtos.ToImmutableList()
            );
        });

        return
            await Task.WhenAll(
                tasks); // <- zbiera wszystkie Task<CompetitionResultDto> w IEnumerable<CompetitionResultDto>
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
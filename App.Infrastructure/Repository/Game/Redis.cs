using System.Text.Json;
using App.Application.Extensions;
using App.Domain.Competition;
using App.Domain.Game;
using Microsoft.FSharp.Core;
using StackExchange.Redis;
using DraftModule = App.Domain.Game.DraftModule;

namespace App.Infrastructure.Repository.Game;

public class Redis(IConnectionMultiplexer redis) : IGames
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"game:live";
    private static string LiveKey(string id) => $"{LivePattern}:{id}";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchivePattern => $"game:archive";
    private static string ArchiveKey(string id) => $"{ArchivePattern}:{id}";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";
    private static string LiveSetKey => $"{LivePattern}:ids";
    private static string ArchiveSetKey => $"{ArchivePattern}:ids";

    public async Task Add(Domain.Game.Game game, CancellationToken ct)
    {
        var gameId = game.Id_.Item;
        var mapperInput = await CreateMapperInput(game);
        var dto = GameDtoMapper.Create(mapperInput);
        var serializedGames = JsonSerializer.Serialize(dto);
        if (game.StatusTag.IsEndedTag)
        {
            await _db.StringSetAsync(ArchiveKey(gameId), serializedGames);
            await _db.SetAddAsync(ArchiveSetKey, dto.Id.ToString());
        }
        else
        {
            await _db.StringSetAsync(LiveKey(gameId), serializedGames);
            await _db.SetAddAsync(LiveSetKey, dto.Id.ToString());
        }
    }

    public async Task<FSharpOption<Domain.Game.Game>> GetById(GameId gameId, CancellationToken ct)
    {
        var liveGameJson = await _db.StringGetAsync(LiveKey(gameId.Item));
        if (liveGameJson.HasValue)
        {
            var dto = JsonSerializer.Deserialize<GameDto>(liveGameJson!);
            if (dto is null)
            {
                throw new Exception("Failed to deserialize live game");
            }

            return dto.ToDomain();
        }

        var archivedGameJson = await _db.StringGetAsync(ArchiveKey(gameId.Item));
        if (archivedGameJson.HasValue)
        {
            var dto = JsonSerializer.Deserialize<GameDto>(archivedGameJson!);
            if (dto is null)
            {
                throw new Exception("Failed to deserialize archived game");
            }

            return dto.ToDomain();
        }

        throw new KeyNotFoundException($"Game {gameId} not found");
    }

    public async Task<IEnumerable<Domain.Game.Game>> GetNotStarted(CancellationToken ct)
    {
        var ids = await _db.SetMembersAsync(LiveSetKey);
        var matchmakings = new List<Domain.Game.Game>();
        foreach (var id in ids)
        {
            if (!id.HasValue) continue;
            var json = await _db.StringGetAsync(LiveKey(id.ToString()));
            if (!json.HasValue) continue;
            var dto = JsonSerializer.Deserialize<GameDto>(json!);
            if (dto is { NextStatus: not null } && dto.NextStatus.Contains("PreDraft"))
                matchmakings.Add(dto.ToDomain());
        }

        return matchmakings;
    }

    public async Task<IEnumerable<Domain.Game.Game>> GetInProgress(CancellationToken ct)
    {
        var ids = await _db.SetMembersAsync(LiveSetKey);
        var matchmakings = new List<Domain.Game.Game>();
        foreach (var id in ids)
        {
            if (!id.HasValue) continue;
            var json = await _db.StringGetAsync(LiveKey(id.ToString()));
            if (!json.HasValue) continue;
            var dto = JsonSerializer.Deserialize<GameDto>(json!);
            if (dto != null && dto.Status != "Ended")
                matchmakings.Add(dto.ToDomain());
        }

        return matchmakings;
    }

    public async Task<int> GetInProgressCount(CancellationToken ct)
    {
        return (int)await _db.SetLengthAsync(LiveSetKey);
    }

    public async Task<IEnumerable<Domain.Game.Game>> GetEnded(CancellationToken ct)
    {
        var ids = await _db.SetMembersAsync(ArchiveSetKey);
        var matchmakings = new List<Domain.Game.Game>();
        foreach (var id in ids)
        {
            if (!id.HasValue) continue;
            var json = await _db.StringGetAsync(ArchiveKey(id.ToString()));
            if (!json.HasValue) continue;
            var dto = JsonSerializer.Deserialize<GameDto>(json!);
            if (dto != null && dto.Status == "Ended")
                matchmakings.Add(dto.ToDomain());
        }

        return matchmakings;
    }

    private async Task<GameDtoMapperInput> CreateMapperInput(Domain.Game.Game game)
    {
        // TODO
        return new GameDtoMapperInput(game, null, null, null, null, null);
    }
}

// `PreDraftEndedCompetitions` prawdopodobnie pochodzić będzie z archiwum, które jest asynchroniczne
public record GameDtoMapperInput(
    Domain.Game.Game Game,
    string? NextStatus,
    List<EndedCompetitionDto>? PreDraftEndedCompetitions,
    int? NextCompetitionJumpInMs,
    List<PlayerPicksDto>? EndedDraftPicks,
    EndedCompetitionDto? EndedMainCompetition);

public static class GameDtoMapper
{
    public static GameDto Create(GameDtoMapperInput input)
    {
        var game = input.Game;

        var gameId = game.Id.Item;
        var dto = new GameDto(
            gameId,
            game.StatusTag.ToString(),
            input.NextStatus,
            CreateSettings(input),
            CreatePlayers(PlayersModule.toList(game.Players)),
            CreatePreDraft(input),
            CreateDraft(input),
            CreateMainCompetition(input),
            game.PhaseHasEnded(StatusTag.MainCompetitionTag) ? input.EndedMainCompetition : null,
            CreateGameRanking(input)
        );
        return dto;
    }

    private static SettingsDto CreateSettings(GameDtoMapperInput input)
    {
        var game = input.Game;
        var breakSettings = game.Settings.BreakSettings;

        var preDraftCompetitionSettings = game.Settings.PreDraftSettings.Competitions_;
        var preDraftCompetitions = preDraftCompetitionSettings.Select(CreateCompetitionSettingsDto).ToList();

        var draftSettings = game.Settings.DraftSettings;

        var uniqueJumpersPolicy = draftSettings.UniqueJumpersPolicy switch
        {
            { IsUnique: true } => "Unique",
            { IsNotUnique: true } => "NotUnique",
            _ => throw new Exception("Unknown unique jumpers policy")
        };

        var orderPolicy = draftSettings.Order switch
        {
            { IsClassic: true } => "Classic",
            { IsSnake: true } => "Snake",
            { IsRandom: true } => "Random",
            _ => throw new Exception("Unknown order policy")
        };

        string timeoutPolicyString;
        switch (draftSettings.TimeoutPolicy)
        {
            case { IsNoTimeout: true }:
                timeoutPolicyString = "NoTimeout";
                break;
            case { IsTimeoutAfter: true } timeoutPolicy:
                var timeoutAfter = ((DraftModule.SettingsModule.TimeoutPolicy.TimeoutAfter)timeoutPolicy).Time
                    .TotalMilliseconds;
                var intTimeoutAfter = (int)Math.Floor(timeoutAfter);
                timeoutPolicyString = $"TimeoutAfter {intTimeoutAfter}";
                break;
            default:
                throw new Exception("Unknown timeout policy");
        }

        var rankingPolicyString = game.Settings.RankingPolicy switch
        {
            { IsClassic: true } => "IsClassic",
            { IsPodiumAtAllCosts: true } => "PodiumAtAllCosts",
            _ => throw new Exception("Unknown ranking policy")
        };

        var competitionJumpIntervalMs = (int)Math.Floor(game.Settings.CompetitionJumpInterval.Value.TotalMilliseconds);

        return new SettingsDto(
            GetMilliseconds(breakSettings.BreakBeforePreDraft),
            GetMilliseconds(breakSettings.BreakBetweenPreDraftCompetitions),
            GetMilliseconds(breakSettings.BreakBeforeDraft), GetMilliseconds(breakSettings.BreakBeforeMainCompetition),
            GetMilliseconds(breakSettings.BreakBeforeEnd), preDraftCompetitions,
            DraftModule.SettingsModule.TargetPicksModule.value(draftSettings.TargetPicks),
            DraftModule.SettingsModule.MaxPicksModule.value(draftSettings.MaxPicks), uniqueJumpersPolicy, orderPolicy,
            timeoutPolicyString, rankingPolicyString, competitionJumpIntervalMs);

        int GetMilliseconds(PhaseDuration phaseDuration) => (int)Math.Floor(phaseDuration.Value.TotalMilliseconds);
    }

    private static CompetitionSettingsDto CreateCompetitionSettingsDto(Domain.Competition.Settings settings)
    {
        var rounds = settings.Rounds.Select(CreateCompetitionRoundSettingsDto).ToList();
        return new CompetitionSettingsDto(rounds);
    }

    private static CompetitionRoundSettingsDto CreateCompetitionRoundSettingsDto(
        Domain.Competition.RoundSettings settings)
    {
        var limitType = settings.RoundLimit switch
        {
            Domain.Competition.RoundLimit.Exact => "Exact",
            Domain.Competition.RoundLimit.Soft => "Soft",
            { IsNoneLimit: true } => "None",
            _ => throw new Exception("Unknown limit type")
        };
        int? limitValue = settings.RoundLimit switch
        {
            Domain.Competition.RoundLimit.Exact limit => RoundLimitValueModule.value(limit.Value),
            Domain.Competition.RoundLimit.Soft limit => RoundLimitValueModule.value(limit.Value),
            _ => null,
        };
        return new CompetitionRoundSettingsDto(limitType, limitValue, settings.SortStartlist, settings.ResetPoints);
    }

    private static List<PlayerDto> CreatePlayers(IEnumerable<Domain.Game.Player> players)
    {
        return players.Select(domainPlayer =>
            new PlayerDto(domainPlayer.Id.Item, PlayerModule.NickModule.value(domainPlayer.Nick))).ToList();
    }

    private static PreDraftDto? CreatePreDraft(GameDtoMapperInput input)
    {
        var game = input.Game;
        if (!game.PhaseHasStarted(StatusTag.DraftTag)) return null;

        string? preDraftStatusString = null;
        int? preDraftCompetitionIndex = null;
        CompetitionDto? currentCompetition = null;
        if (game.StatusTag.IsPreDraftTag)
        {
            var preDraftStatus = ((Domain.Game.Status.PreDraft)game.Status).Item;
            (preDraftStatusString, preDraftCompetitionIndex) = preDraftStatus switch
            {
                Domain.Game.PreDraftStatus.Running running => ("Running",
                    (int?)PreDraftCompetitionIndexModule.value(running.Index)),
                Domain.Game.PreDraftStatus.Break before => ("Break " +
                                                            PreDraftCompetitionIndexModule.value(before.NextIndex),
                    null),
                _ => throw new Exception("Unknown pre-draft status")
            };

            if (preDraftStatus.IsRunning)
            {
                var runningPreDraft = (Domain.Game.PreDraftStatus.Running)preDraftStatus;
                currentCompetition = CreateCompetition(input, runningPreDraft.Competition);
            }
        }

        var preDraft = new PreDraftDto(preDraftStatusString, preDraftCompetitionIndex, input.PreDraftEndedCompetitions,
            currentCompetition);
        return preDraft;
    }

    private static CompetitionDto CreateCompetition(GameDtoMapperInput input,
        Domain.Competition.Competition competition)
    {
        var (statusString, roundIndex, gateState) = competition.Status_ switch
        {
            Domain.Competition.CompetitionModule.Status.NotStarted notStarted => ("NotStarted", (int?)0,
                CreateGateState(notStarted.GateState)),
            Domain.Competition.CompetitionModule.Status.RoundInProgress roundInProgress => ("RoundInProgress",
                (int?)RoundIndexModule.value(roundInProgress.RoundIndex),
                CreateGateState(roundInProgress.GateState)),
            Domain.Competition.CompetitionModule.Status.Suspended suspended => ("Suspended", null,
                CreateGateState(suspended.GateState)),
            { IsCancelled: true } => ("Cancelled", null, null),
            { IsEnded: true } => ("Ended", null, null),
            _ => throw new Exception("Unknown competition status")
        };

        var doneEntries = competition.Startlist_.DoneEntries;
        var startlistEntries = competition.Startlist_.FullEntries;
        var startlist = startlistEntries.Select(domainStartlistEntry =>
        {
            var hasDone = doneEntries.Contains(domainStartlistEntry);
            return new StartlistJumperDto(StartlistModule.BibModule.value(domainStartlistEntry.Bib), hasDone,
                domainStartlistEntry.JumperId.Item);
        }).ToList();

        var results = competition.Classification;
        var jumperResultDtos = results.Select(jumperClassificationResult =>
        {
            var rounds = jumperClassificationResult.JumpResults.Select(jumpResult =>
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

                return new CompetitionRoundResultDto(jumpResult.JumperId.Item,
                    JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
                    TotalPointsModule.value(jumpResult.TotalPoints),
                    JumpModule.JudgesModule.value(jumpResult.Jump.JudgeNotes), judgePoints, windPoints,
                    jumpResult.Jump.Wind.ToDouble(), gatePoints,
                    JumpResultModule.TotalCompensationModule.value(jumpResult.TotalCompensation));
            }).ToList();

            var result = new CompetitionResultDto(jumperClassificationResult.JumperId.Item,
                TotalPointsModule.value(jumperClassificationResult.Points),
                Classification.PositionModule.value(jumperClassificationResult.Position), rounds);

            return result;
        }).ToList();
        var competitionDto =
            new CompetitionDto(statusString, input.NextCompetitionJumpInMs, roundIndex, startlist, jumperResultDtos,
                gateState);
        return competitionDto;
    }

    private static GateStateDto CreateGateState(Domain.Competition.GateState gateState)
    {
        int? coachReduction = gateState.CoachChange.IsSome()
            ? gateState.CoachChange.Value.ToInt()
            : null;
        var gateStateDto = new GateStateDto(GateModule.value(gateState.Starting),
            GateModule.value(gateState.CurrentJury), coachReduction);
        return gateStateDto;
    }

    private static DraftDto? CreateDraft(GameDtoMapperInput input)
    {
        var game = input.Game;
        if (!game.PhaseHasStarted(StatusTag.DraftTag)) return null;

        Guid? currentTurnPlayerId = null;
        int? currentTurnIndex = null;
        List<PlayerPicksDto>? picksList = null;
        List<Guid>? nextPlayers = null;
        if (game.StatusTag.IsDraftTag)
        {
            var draftStatus = ((Domain.Game.Status.Draft)game.Status).Item;
            var currentTurn = draftStatus.CurrentTurn;
            if (currentTurn.IsSome())
            {
                currentTurnPlayerId = currentTurn.Value.PlayerId.Item;
                currentTurnIndex = DraftModule.TurnIndexModule.value(currentTurn.Value.Index);
            }

            var picksMap = game.DraftPicks;
            picksList = picksMap.ToList().Select(kvp =>
            {
                var gamePlayerId = kvp.Key.Item;
                var gameJumperIds = kvp.Value.Select(jumperId => jumperId.Item).ToList();
                return new PlayerPicksDto(gamePlayerId, gameJumperIds);
            }).ToList();

            nextPlayers = draftStatus.TurnQueueRemaining.Select(playerId => playerId.Item).ToList();
        }
        else
        {
            if (input.EndedDraftPicks is not null)
            {
                picksList = input.EndedDraftPicks;
            }
            else
            {
                throw new Exception("Ended draft picks not found, but draft is not running");
            }
        }

        var draftIsRunning = game.StatusTag.IsDraftTag;


        var draftDto = new DraftDto(draftIsRunning, currentTurnPlayerId, currentTurnIndex, picksList,
            nextPlayers ?? []);

        return draftDto;
    }

    private static CompetitionDto? CreateMainCompetition(GameDtoMapperInput input)
    {
        var game = input.Game;

        if (!game.StatusTag.IsMainCompetitionTag) return null;

        var mainCompetition = ((Domain.Game.Status.MainCompetition)game.Status).Competition;
        var competitionDto = CreateCompetition(input, mainCompetition);
        return competitionDto;
    }

    private static GameRankingDto? CreateGameRanking(GameDtoMapperInput input)
    {
        var game = input.Game;
        if (!game.PhaseHasEnded(StatusTag.EndedTag)) return null;

        var endedPhase = ((Domain.Game.Status.Ended)game.Status);
        var positionAndPointsMap = endedPhase.Ranking.PositionsAndPoints;
        var positionAndPointsList = positionAndPointsMap.Select(kvp =>
        {
            var gamePlayerGuid = kvp.Key.Item;
            var position = RankingModule.PositionModule.value(kvp.Value.Item1);
            var points = RankingModule.PointsModule.value(kvp.Value.Item2);
            return new GameRankingRecordDto(gamePlayerGuid, position, points);
        }).ToList();

        return new GameRankingDto(positionAndPointsList);
    }

    public static Domain.Game.Game ToDomain(this GameDto dto)
    {
        var game = Domain.Game.Game.Create();
        return game;
    }
}

public record CompetitionRoundSettingsDto(
    string LimitType,
    int? Limit,
    bool SortStartlist,
    bool ResetPoints
);

public record CompetitionSettingsDto(
    List<CompetitionRoundSettingsDto> Rounds
);

/// <summary>
/// Breaks are in milliseconds
/// </summary>
public record SettingsDto(
    int BreakBeforePreDraftMs,
    int BreakBetweenPreDraftCompetitionsMs,
    int BreakBeforeDraftMs,
    int BreakBeforeMainCompetitionMs,
    int BreakBeforeEndMs,
    List<CompetitionSettingsDto> PreDraftCompetitions,
    int DraftTargetPicks,
    int DraftMaxPicks,
    string UniqueJumpersPolicy,
    string OrderPolicy,
    string TimeoutPolicy,
    string RankingPolicy,
    int CompetitionJumpIntervalMs);

public record CompetitionDto(
    // TODO
    string Status, // "NotStarted" | "RoundInProgress" | "Ended"
    int? NextJumpInMs,
    int? RoundIndex,
    List<StartlistJumperDto> Startlist,
    List<CompetitionResultDto> Results,
    GateStateDto? GateState
);

public record EndedCompetitionDto(
    List<CompetitionResultDto> Results
);

public sealed record StartlistJumperDto(
    int Bib,
    bool Done,
    Guid CompetitionJumperId
);

public sealed record CompetitionResultDto(
    Guid CompetitionJumperId,
    // int Bib,
    double Total,
    double Rank,
    IReadOnlyList<CompetitionRoundResultDto> RoundResults
);

public sealed record CompetitionRoundResultDto(
    // Guid GameJumperId,
    Guid CompetitionJumperId,
    double Distance,
    double Points,
    IReadOnlyList<double>? Judges,
    double? JudgePoints,
    double? WindCompensation,
    double WindAverage,
    double? GateCompensation,
    double? TotalCompensation
);

public sealed record CompetitionJumperDto(
    Guid GameJumperId,
    Guid CompetitionJumperId,
    string Name,
    string Surname,
    string CountryFisCode
);

public sealed record GateStateDto(
    int Starting,
    int CurrentJury,
    int? CoachReduction
);

public record PreDraftDto(
    string? Status,
    int? Index,
    List<EndedCompetitionDto>? EndedCompetitions,
    CompetitionDto? CurrentCompetition
);

public record PlayerPicksDto(Guid GamePlayerId, List<Guid> GameJumperIds);

public record DraftDto(
    bool IsRunning,
    Guid? CurrentTurnPlayerId,
    int? CurrentTurnIndex,
    List<PlayerPicksDto> Picks,
    // List<Guid> AvailableGameJumpers,
    List<Guid> NextPlayers);

public record PlayerDto(Guid Id, string Nick);

public record GameRankingRecordDto(
    Guid GamePlayerId,
    int Rank,
    int Points
);

public record GameRankingDto(
    List<GameRankingRecordDto> Records);

public record GameDto(
    Guid Id,
    string Status,
    string? NextStatus,
    SettingsDto Settings,
    List<PlayerDto> Players,
    PreDraftDto? PreDraft,
    DraftDto? Draft,
    CompetitionDto? MainCompetition,
    EndedCompetitionDto? EndedMainCompetition,
    GameRankingDto? Ranking);
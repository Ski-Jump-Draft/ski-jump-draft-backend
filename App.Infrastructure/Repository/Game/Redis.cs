using System.Text.Json;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.DraftPicks;
using App.Application.Game.GameCompetitions;
using App.Application.Utility;
using App.Domain.Competition;
using App.Domain.Game;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using StackExchange.Redis;
using DraftModule = App.Domain.Game.DraftModule;
using HillId = App.Domain.Competition.HillId;
using JumperId = App.Domain.Game.JumperId;
using RankingModule = App.Domain.Game.RankingModule;
using StartlistModule = App.Domain.Competition.StartlistModule;

namespace App.Infrastructure.Repository.Game;

public class Redis(
    IConnectionMultiplexer redis,
    IGameSchedule gameSchedule,
    IClock clock,
    IMyLogger logger,
    IGameCompetitionResultsArchive gameCompetitionResultsArchive,
    IDraftPicksArchive draftPicksArchive) : IGames
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"game:live";
    private static string LiveKey(string id) => $"{LivePattern}:{id}";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchivePattern => $"game:archive";
    private static string ArchiveKey(string id) => $"{ArchivePattern}:{id}";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";
    private static string LiveSetKey => $"{LivePattern}:ids";
    private static string ArchiveSetKey => $"{ArchivePattern}:ids";
    private static string LiveHashKey => "game:live";
    private static string ArchiveHashKey => "game:archive";

    private async Task<IEnumerable<Domain.Game.Game>> GetGamesFromHash(
        string cacheKey,
        string hashKey,
        Func<GameDto, Guid, bool> filter,
        TimeSpan ttl,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Domain.Game.Game>? cached))
            return cached!;

        var entries = await _db.HashGetAllAsync(hashKey);
        if (entries.Length == 0) return [];

        var result = new List<Domain.Game.Game>();
        var toRemove = new List<RedisValue>();

        foreach (var entry in entries)
        {
            var guid = Guid.Parse(entry.Name!);
            if (!entry.Value.HasValue)
            {
                toRemove.Add(entry.Name);
                continue;
            }

            var dto = JsonSerializer.Deserialize<GameDto>(entry.Value!);
            if (dto is null) continue;

            if (filter(dto, guid))
                result.Add(dto.ToDomain(GetNextGameStatus(guid)));
        }

        if (toRemove.Count > 0)
            await _db.HashDeleteAsync(hashKey, toRemove.ToArray());

        _cache.Set(cacheKey, result, ttl);
        return result;
    }

    private async Task<int> GetGameCountFromHash(
        string cacheKey,
        string hashKey,
        TimeSpan ttl,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(cacheKey, out int cached))
            return cached;

        var count = (int)await _db.HashLengthAsync(hashKey);
        _cache.Set(cacheKey, count, ttl);
        return count;
    }

    public async Task Add(Domain.Game.Game game, CancellationToken ct)
    {
        var gameId = game.Id_.Item;
        var dto = GameDtoMapper.Create(await CreateMapperInput(game, ct), clock.Now());
        var json = JsonSerializer.Serialize(dto);

        if (game.StatusTag.IsEndedTag)
        {
            await _db.HashSetAsync(ArchiveHashKey, gameId.ToString(), json);
            await _db.HashDeleteAsync(LiveHashKey, gameId.ToString());
        }
        else
        {
            await _db.HashSetAsync(LiveHashKey, gameId.ToString(), json);
        }
    }

    public async Task<FSharpOption<Domain.Game.Game>> GetById(GameId gameId, CancellationToken ct)
    {
        var cacheKey = $"game:{gameId.Item}";
        if (_cache.TryGetValue(cacheKey, out FSharpOption<Domain.Game.Game>? cached))
            return cached!;

        var value = await _db.HashGetAsync(LiveHashKey, gameId.Item.ToString());
        if (!value.HasValue)
            value = await _db.HashGetAsync(ArchiveHashKey, gameId.Item.ToString());

        if (!value.HasValue)
        {
            _cache.Set(cacheKey, FSharpOption<Domain.Game.Game>.None, TimeSpan.FromSeconds(3));
            throw new KeyNotFoundException($"Game {gameId} not found");
        }

        var dto = JsonSerializer.Deserialize<GameDto>(value!)
                  ?? throw new Exception("Failed to deserialize GameDto");
        var domain = dto.ToDomain(GetNextGameStatus(gameId.Item));
        _cache.Set(cacheKey, domain, TimeSpan.FromSeconds(3));
        return domain;
    }

    public Task<IEnumerable<Domain.Game.Game>> GetNotStarted(CancellationToken ct) =>
        GetGamesFromHash("GetNotStarted", LiveHashKey,
            (dto, guid) => GetNextScheduledGamePhase(guid) == GameScheduleTarget.PreDraft,
            TimeSpan.FromSeconds(3), ct);

    public Task<IEnumerable<Domain.Game.Game>> GetInProgress(CancellationToken ct) =>
        GetGamesFromHash("GetInProgress", LiveHashKey,
            (dto, _) => dto.Status != "Ended",
            TimeSpan.FromSeconds(3), ct);

    public Task<IEnumerable<Domain.Game.Game>> GetEnded(CancellationToken ct) =>
        GetGamesFromHash("GetEnded", ArchiveHashKey,
            (dto, _) => dto.Status == "Ended",
            TimeSpan.FromSeconds(5), ct);

    public Task<int> GetInProgressCount(CancellationToken ct) =>
        GetGameCountFromHash("GetInProgressCount", LiveHashKey,
            TimeSpan.FromSeconds(2), ct);

    private async Task<GameDtoMapperInput> CreateMapperInput(Domain.Game.Game game, CancellationToken ct)
    {
        var gameGuid = game.Id.Item;


        var preDraftTask = gameCompetitionResultsArchive.GetPreDraftResultsAsync(gameGuid, ct);
        var mainTask = gameCompetitionResultsArchive.GetMainResultsAsync(gameGuid, ct);
        var picksTask = GetArchivedDraftPicks(gameGuid);

        await Task.WhenAll(preDraftTask, mainTask, picksTask);

        var preDraftResults = await preDraftTask;
        var mainCompetitionResults = await mainTask;
        var draftPicksList = await picksTask;

        var preDraftEndedCompetitions = preDraftResults?
            .Select(CreateEndedCompetitionFromArchive)
            .ToList();

        var endedMainCompetition = mainCompetitionResults != null
            ? CreateEndedCompetitionFromArchive(mainCompetitionResults)
            : null;

        return new GameDtoMapperInput(
            game,
            preDraftEndedCompetitions,
            GetNextCompetitionJumpInMs(gameGuid),
            draftPicksList,
            endedMainCompetition
        );
    }


    private async Task<List<PlayerPicksDto>?> GetArchivedDraftPicks(Guid gameGuid)
    {
        var endedDraftPicks = (await draftPicksArchive.GetPicks(gameGuid))?.ToList();
        var draftPicksList = endedDraftPicks?.Select(kvp =>
        {
            return new PlayerPicksDto(kvp.Key.Item, kvp.Value.Select(id => id.Item).ToList());
        }).ToList();
        return draftPicksList;
    }

    private EndedCompetitionDto CreateEndedCompetitionFromArchive(
        ArchiveCompetitionResultsDto archiveCompetitionResultsDto)
    {
        var jumperResults = archiveCompetitionResultsDto.JumperResults.Select(result =>
        {
            return new CompetitionResultDto(result.CompetitionJumperId, result.Bib, result.Points, result.Rank,
                result.Jumps.Select((archiveJumpResult, index) => new CompetitionRoundResultDto(archiveJumpResult.Id,
                    archiveJumpResult.CompetitionJumperId, index,
                    archiveJumpResult.Distance, archiveJumpResult.Points, archiveJumpResult.Judges,
                    archiveJumpResult.JudgePoints, archiveJumpResult.WindCompensation,
                    archiveJumpResult.WindAverage, archiveJumpResult.Gate, archiveJumpResult.GateCompensation,
                    archiveJumpResult.TotalCompensation)).ToList());
        });
        return new EndedCompetitionDto(jumperResults.ToList());
    }

    private Application.Game.GameScheduleTarget? GetNextScheduledGamePhase(Guid gameId)
    {
        var schedule = gameSchedule.GetGameSchedule(gameId);
        return schedule?.ScheduleTarget;
    }

    private int? GetNextCompetitionJumpInMs(Guid gameId)
    {
        var schedule = gameSchedule.GetGameSchedule(gameId);
        if (schedule?.ScheduleTarget == GameScheduleTarget.CompetitionJump)
        {
            return (int)Math.Floor(schedule.In.TotalMilliseconds);
        }

        return null;
    }

    private string? GetNextGameStatus(Guid gameId)
    {
        var schedule = gameSchedule.GetGameSchedule(gameId);
        return schedule?.ScheduleTarget != GameScheduleTarget.CompetitionJump
            ? $"{schedule!.ScheduleTarget.ToString()} IN {(int)Math.Floor(schedule!.In.TotalMilliseconds)}"
            : null;
    }

    private string? GetNextGameStatus(Application.Game.GameScheduleDto schedule)
    {
        string? nextStatus = null;
        var now = clock.Now();
        var nextStatusExistsAndIsValid = !schedule.BreakPassed(now) &&
                                         schedule.ScheduleTarget != GameScheduleTarget.CompetitionJump;
        if (nextStatusExistsAndIsValid)
        {
            nextStatus = $"{schedule!.ScheduleTarget.ToString()} IN {(int)Math.Floor(schedule!.In.TotalMilliseconds)}";
        }

        return nextStatus;
    }

    private int? GetNextCompetitionJumpInMs(Application.Game.GameScheduleDto schedule)
    {
        if (schedule.ScheduleTarget == GameScheduleTarget.CompetitionJump)
        {
            return (int)Math.Floor(schedule.In.TotalMilliseconds);
        }

        return null;
    }
}

// `PreDraftEndedCompetitions` prawdopodobnie pochodzić będzie z archiwum, które jest asynchroniczne
public record GameDtoMapperInput(
    Domain.Game.Game Game,
    // string? NextStatus,
    List<EndedCompetitionDto>? PreDraftEndedCompetitions,
    int? NextCompetitionJumpInMs,
    List<PlayerPicksDto>? EndedDraftPicks,
    EndedCompetitionDto? EndedMainCompetition);

public static class GameDtoMapper
{
    public static GameDto Create(GameDtoMapperInput input, DateTimeOffset now)
    {
        var game = input.Game;

        var jumpers = JumpersModule.toList(game.Jumpers).Select(jumper => new JumperDto(jumper.Id.Item)).ToList();
        var gameJumperIds = JumpersModule.toIdsList(game.Jumpers).Select(id => id.Item).ToList();

        var players = CreatePlayers(PlayersModule.toList(game.Players));
        var playersOrder = players.Select(player => player.Id).ToList();

        var gameId = game.Id.Item;
        var dto = new GameDto(
            gameId,
            now,
            game.StatusTag.ToString().RemoveFromEndInWordsIfPresent("Tag"),
            CreateSettings(input),
            CreateCompetitionHill(input),
            players,
            jumpers,
            CreatePreDraft(input),
            CreateDraft(input, gameJumperIds, playersOrder),
            CreateMainCompetition(input),
            game.PhaseHasEnded(StatusTag.MainCompetitionTag) ? input.EndedMainCompetition : null,
            CreateGameRanking(input)
        );
        return dto;
    }

    private static CompetitionHillDto CreateCompetitionHill(GameDtoMapperInput input)
    {
        var game = input.Game;
        if (game.Hill.IsNone())
        {
            throw new Exception("Hill is None");
        }

        var hill = game.Hill.Value;

        return new CompetitionHillDto(hill.Id.Item, HillModule.KPointModule.value(hill.KPoint),
            HillModule.HsPointModule.value(hill.HsPoint), HillModule.GatePointsModule.value(hill.GatePoints),
            HillModule.WindPointsModule.value(hill.HeadwindPoints),
            HillModule.WindPointsModule.value(hill.TailwindPoints));
    }

    private static SettingsDto CreateSettings(GameDtoMapperInput input)
    {
        var game = input.Game;
        var breakSettings = game.Settings.BreakSettings;

        var preDraftCompetitionSettings = game.Settings.PreDraftSettings.Competitions_;
        var preDraftCompetitions = preDraftCompetitionSettings.Select(CreateCompetitionSettingsDto).ToList();

        var mainCompetitionSettings = CreateCompetitionSettingsDto(game.Settings.MainCompetitionSettings);

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
            GetMilliseconds(breakSettings.BreakBeforeEnd), preDraftCompetitions, mainCompetitionSettings,
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
        if (!game.PhaseHasStarted(StatusTag.PreDraftTag)) return null;

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

        var bibsByJumperId = competition.Bibs.ToDictionary();


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

                return new CompetitionRoundResultDto(jumpResult.Id.Item, jumpResult.JumperId.Item,
                    (int)jumpResult.RoundIndex.Item,
                    JumpModule.DistanceModule.value(jumpResult.Jump.Distance),
                    TotalPointsModule.value(jumpResult.TotalPoints),
                    JumpModule.JudgesModule.value(jumpResult.Jump.JudgeNotes), judgePoints, windPoints,
                    jumpResult.Jump.Wind.ToDouble(), jumpResult.Jump.Gate.Item, gatePoints,
                    JumpResultModule.TotalCompensationModule.value(jumpResult.TotalCompensation));
            }).ToList();

            if (!bibsByJumperId.TryGetValue(jumperClassificationResult.JumperId, out var bib))
            {
                throw new Exception("Bib not found for jumper");
            }

            var result = new CompetitionResultDto(jumperClassificationResult.JumperId.Item,
                StartlistModule.BibModule.value(bib), TotalPointsModule.value(jumperClassificationResult.Points),
                Classification.PositionModule.value(jumperClassificationResult.Position), rounds);

            return result;
        }).ToList();

        var jumpers = bibsByJumperId.Keys
            .Select(jumperId => new CompetitionJumperDto(jumperId.Item))
            .ToList();


        var competitionDto = new CompetitionDto(
            competition.Id_.Item,
            statusString,
            input.NextCompetitionJumpInMs,
            roundIndex,
            jumpers,
            startlist,
            jumperResultDtos,
            gateState
        );

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

    private static DraftDto? CreateDraft(GameDtoMapperInput input, List<Guid> jumperIds, List<Guid> playersOrder)
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

        var draftDto = new DraftDto(draftIsRunning, currentTurnPlayerId, currentTurnIndex, jumperIds, playersOrder,
            nextPlayers ?? [],
            picksList);

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
        if (!game.StatusTag.IsEndedTag) return null;

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

    public static Domain.Game.Game ToDomain(this GameDto dto, string? nextGameStatus)
    {
        var preDraftCompetitions = dto.Settings.PreDraftCompetitions.Select(DomainCreateCompetitionSettings).ToList();
        var preDraftSettings = PreDraftSettings.Create(ListModule.OfSeq(preDraftCompetitions)).Value;
        var uniqueJumpersPolicy = dto.Settings.DraftUniqueJumpersPolicy switch
        {
            "Unique" => Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.Unique,
            "NotUnique" => Domain.Game.DraftModule.SettingsModule.UniqueJumpersPolicy.NotUnique,
            _ => throw new Exception("Unknown unique jumpers policy")
        };
        var orderPolicy = dto.Settings.DraftOrderPolicy switch
        {
            "Classic" => Domain.Game.DraftModule.SettingsModule.Order.Classic,
            "Snake" => Domain.Game.DraftModule.SettingsModule.Order.Snake,
            "Random" => Domain.Game.DraftModule.SettingsModule.Order.Random,
            _ => throw new Exception("Unknown order policy")
        };
        DraftModule.SettingsModule.TimeoutPolicy timeoutPolicy;
        switch (dto.Settings.DraftTimeoutPolicy)
        {
            case "NoTimeout":
                timeoutPolicy = Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NoTimeout;
                break;
            case var timeoutAfter when timeoutAfter.StartsWith("TimeoutAfter "):
                if (int.TryParse(timeoutAfter.AsSpan("TimeoutAfter ".Length), out var timeoutInMs))
                    timeoutPolicy =
                        Domain.Game.DraftModule.SettingsModule.TimeoutPolicy.NewTimeoutAfter(
                            TimeSpan.FromMilliseconds(timeoutInMs));
                else
                    throw new FormatException($"Invalid TimeoutAfter: {timeoutAfter}");
                break;
            default:
                throw new Exception("Unknown timeout policy");
        }

        var rankingPolicy = dto.Settings.DraftRankingPolicy switch
        {
            "IsClassic" => Domain.Game.RankingPolicy.Classic,
            "PodiumAtAllCosts" => Domain.Game.RankingPolicy.PodiumAtAllCosts,
            _ => throw new Exception("Unknown ranking policy")
        };

        var draftSettings = new Domain.Game.DraftModule.Settings(
            DraftModule.SettingsModule.TargetPicksModule.create(dto.Settings.DraftTargetPicks).Value,
            DraftModule.SettingsModule.MaxPicksModule.create(dto.Settings.DraftMaxPicks).Value,
            uniqueJumpersPolicy, orderPolicy, timeoutPolicy
        );
        var mainCompetitionSettings = DomainCreateCompetitionSettings(dto.Settings.MainCompetition);
        var settings = new Domain.Game.Settings(
            new Domain.Game.BreakSettings(
                PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.BreakBeforePreDraftMs)),
                PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.BreakBetweenPreDraftCompetitionsMs)),
                PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.BreakBeforeDraftMs)),
                PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.BreakBeforeMainCompetitionMs)),
                PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.BreakBeforeEndMs))
            ), preDraftSettings, draftSettings, mainCompetitionSettings,
            PhaseDuration.Create(TimeSpan.FromMilliseconds(dto.Settings.CompetitionJumpIntervalMs)), rankingPolicy
        );
        var players = dto.Players.Select(playerDto => new Domain.Game.Player(PlayerId.NewPlayerId(playerDto.Id),
            PlayerModule.NickModule.create(playerDto.Nick).Value));
        var wrappedPlayers = PlayersModule.create(ListModule.OfSeq(players)).ResultValue;
        var jumpers =
            dto.Jumpers.Select(jumperDto => new Domain.Game.Jumper(JumperId.NewJumperId(jumperDto.GameJumperId)))
                .ToList();
        var jumperIds = ListModule.OfSeq(jumpers.Select(jumper => jumper.Id));
        var wrappedJumpers = JumpersModule.create(ListModule.OfSeq(jumpers));

        var hillDto = dto.CompetitionHillDto;
        var hill = new Domain.Competition.Hill(HillId.NewHillId(hillDto.Id),
            HillModule.KPointModule.tryCreate(hillDto.KPoint).Value,
            HillModule.HsPointModule.tryCreate(hillDto.HsPoint).Value,
            HillModule.GatePointsModule.tryCreate(hillDto.GatePoints).Value,
            HillModule.WindPointsModule.tryCreate(hillDto.HeadwindPoints).Value,
            HillModule.WindPointsModule.tryCreate(hillDto.TailwindPoints).Value);

        var gameStatus =
            DomainCreateGameStatus(dto, nextGameStatus, preDraftSettings, draftSettings, mainCompetitionSettings, hill,
                jumperIds);

        var game = Domain.Game.Game.CreateFromState(Domain.Game.GameId.NewGameId(dto.Id), settings, wrappedPlayers,
            wrappedJumpers, hill, gameStatus);
        if (game.IsError)
        {
            throw new Exception(game.ErrorValue.ToString());
        }

        return game.ResultValue;
    }

    private static Domain.Competition.Settings DomainCreateCompetitionSettings(
        CompetitionSettingsDto competitionSettingsDto)
    {
        var rounds = competitionSettingsDto.Rounds.Select(roundSettings =>
        {
            var limit = (roundSettings.LimitType, roundSettings.Limit) switch
            {
                ("Exact", { } limitValue) => Domain.Competition.RoundLimit.NewExact(
                    RoundLimitValueModule.tryCreate(limitValue).ResultValue),
                ("Soft", { } limitValue) => Domain.Competition.RoundLimit.NewSoft(
                    RoundLimitValueModule.tryCreate(limitValue).ResultValue),
                ("None", _) => Domain.Competition.RoundLimit.NoneLimit,
                _ => throw new Exception("Unknown limit type")
            };
            return new Domain.Competition.RoundSettings(limit, roundSettings.SortStartlist, roundSettings.ResetPoints);
        });
        var settings = Domain.Competition.Settings.Create(ListModule.OfSeq(rounds));
        if (settings.IsError)
        {
            throw new Exception(settings.ErrorValue.ToString());
        }

        return settings.ResultValue;
    }

    private static Domain.Competition.Competition DomainCreateCompetition(GameDto gameDto,
        CompetitionDto competitionDto, Domain.Competition.Settings settings, Domain.Competition.Hill hill)
    {
        var gameId = gameDto.Id;
        var competitionId = competitionDto.Id;
// Startlist (wszyscy, którzy są w aktualnej kolejce/już skakali)
        var startlist = competitionDto.Startlist;

// Bibs z Startlist
        var startlistBibs = startlist
            .Select(s => Tuple.Create(
                Domain.Competition.JumperId.NewJumperId(s.CompetitionJumperId),
                StartlistModule.BibModule.create(s.Bib).Value))
            .ToDictionary(t => t.Item1, t => t.Item2);

// Bibs z Results (mogą być tylko część, ale lepiej mieć nadpisane z tego źródła)
        var resultsBibs = (competitionDto.Results ?? new List<CompetitionResultDto>())
            .Select(r => Tuple.Create(
                Domain.Competition.JumperId.NewJumperId(r.CompetitionJumperId),
                StartlistModule.BibModule.create(r.Bib).Value))
            .ToDictionary(t => t.Item1, t => t.Item2);

// Union obu słowników
        var allBibs = startlistBibs
            .Concat(resultsBibs)
            .GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.First().Value);

// Zamiana na listę dla CompetitionResume
        var bibs = allBibs
            .Select(kv => Tuple.Create(kv.Key, kv.Value))
            .ToList();


        var doneJumpers = startlist
            .Where(startlistJumperDto => startlistJumperDto.Done)
            .Select(j => Domain.Competition.JumperId.NewJumperId(j.CompetitionJumperId))
            .ToList();

        var remainingJumpers = startlist
            .Select(startlistJumperDto =>
                Domain.Competition.JumperId.NewJumperId(startlistJumperDto.CompetitionJumperId))
            .Except(doneJumpers)
            .ToList();

        var results = competitionDto.Results;
        var roundResultDtos = results.SelectMany(result => result.RoundResults).ToList();
        var jumpResults = roundResultDtos.Select(roundResult =>
        {
            var jump = new Domain.Competition.Jump(
                Domain.Competition.JumperId.NewJumperId(roundResult.CompetitionJumperId),
                JumpModule.DistanceModule.tryCreate(roundResult.Distance).ResultValue,
                JumpModule.JudgesModule.tryCreate(ListModule.OfSeq(roundResult.Judges)).Value,
                JumpModule.WindAverage.FromDouble(roundResult.WindAverage), Gate.NewGate(roundResult.Gate));
            var roundIndex = RoundIndex.NewRoundIndex((uint)roundResult.RoundIndex);
            var judgePoints = roundResult.JudgePoints is not null
                ? JumpResultModule.JudgePointsModule.tryCreate(roundResult.JudgePoints.Value).ResultValue
                : null;
            var gatePoints = roundResult.GateCompensation is not null
                ? JumpResultModule.GatePoints.NewGatePoints(roundResult.GateCompensation.Value)
                : null;
            var windPoints = roundResult.WindCompensation is not null
                ? JumpResultModule.WindPoints.NewWindPoints(roundResult.WindCompensation.Value)
                : null;
            var totalPoints = TotalPoints.NewTotalPoints(roundResult.Points);
            var jumpResult = new Domain.Competition.JumpResult(JumpResultId.NewJumpResultId(roundResult.JumpResultId),
                Domain.Competition.JumperId.NewJumperId(roundResult.CompetitionJumperId), jump, roundIndex, judgePoints,
                gatePoints, windPoints, totalPoints);
            return jumpResult;
        }).ToList();

        var (statusTag, gateState) = (competitionDto.Status, competitionDto.GateState) switch
        {
            ("NotStarted", { } gateStateDto) => (CompetitionModule.StatusTag.NotStartedTag,
                CreateDomainGateState(gateStateDto.Starting, gateStateDto.CurrentJury, gateStateDto.CoachReduction)),
            ("RoundInProgress", { } gateStateDto) => (CompetitionModule.StatusTag.RoundInProgressTag,
                CreateDomainGateState(gateStateDto.Starting, gateStateDto.CurrentJury, gateStateDto.CoachReduction)),
            ("Suspended", { } gateStateDto) => (CompetitionModule.StatusTag.SuspendedTag,
                CreateDomainGateState(gateStateDto.Starting, gateStateDto.CurrentJury, gateStateDto.CoachReduction)),
            ("Ended", _) => (CompetitionModule.StatusTag.EndedTag, null),
            ({ } breakStatus, _) when breakStatus.StartsWith("Break ") => (
                CreateRawStatusTag(breakStatus.Substring("Break ".Length)),
                null),
            _ => throw new Exception("Unknown status: " + competitionDto.Status)
        };

        var roundIndex = competitionDto.RoundIndex is not null
            ? RoundIndex.NewRoundIndex((uint)competitionDto.RoundIndex.Value)
            : null;

        var jumperIdsFromBibs = new HashSet<Guid>(bibs.Select(t => t.Item1.Item));
        var jumpersList = competitionDto.Jumpers.ToList();

        if (jumpersList.Count != jumperIdsFromBibs.Count)
        {
            var known = new HashSet<Guid>(jumpersList.Select(j => j.Id));
            foreach (var id in jumperIdsFromBibs)
            {
                if (!known.Contains(id))
                    jumpersList.Add(new CompetitionJumperDto(id));
            }
        }

        var competitionResume = new CompetitionResume(
            CompetitionId.NewCompetitionId(competitionId),
            settings,
            hill,
            ListModule.OfSeq(jumpersList.Select(competitionJumperDto =>
                new Domain.Competition.Jumper(Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id)))),
            ListModule.OfSeq(bibs),
            ListModule.OfSeq(doneJumpers),
            ListModule.OfSeq(remainingJumpers),
            ListModule.OfSeq(jumpResults),
            statusTag,
            gateState,
            roundIndex
        );


        // var competitionResume = new CompetitionResume(CompetitionId.NewCompetitionId(competitionId), settings, hill,
        //     ListModule.OfSeq(competitionJumpers), ListModule.OfSeq(bibs), ListModule.OfSeq(doneJumpers),
        //     ListModule.OfSeq(remainingJumpers), ListModule.OfSeq(jumpResults), statusTag, gateState, roundIndex);
        var competition = Domain.Competition.Competition.Restore(competitionResume);
        if (competition.IsError)
        {
            throw new Exception(competition.ErrorValue.ToString());
        }

        return competition.ResultValue;

        Domain.Competition.CompetitionModule.StatusTag CreateRawStatusTag(string statusString)
        {
            return statusString switch
            {
                "NotStarted" => CompetitionModule.StatusTag.NotStartedTag,
                "RoundInProgress" => CompetitionModule.StatusTag.RoundInProgressTag,
                "Suspended" => CompetitionModule.StatusTag.SuspendedTag,
                "Cancelled" => CompetitionModule.StatusTag.CancelledTag,
                "Ended" => CompetitionModule.StatusTag.EndedTag,
                _ => throw new Exception("Unknown status")
            };
        }

        Domain.Competition.GateState CreateDomainGateState(int starting, int currentJury, int? coachReduction)
        {
            return new Domain.Competition.GateState(Gate.NewGate(starting), Gate.NewGate(currentJury),
                coachReduction > 0 ? GateChange.CreateReduction((uint)coachReduction.Value) : null);
        }
    }

    private static Domain.Game.Status DomainCreateGameStatus(GameDto gameDto, string? nextStatus,
        PreDraftSettings preDraftSettings,
        DraftModule.Settings draftSettings, Domain.Competition.Settings mainCompetitionSettings,
        Domain.Competition.Hill hill,
        FSharpList<JumperId> jumperIds)
    {
        switch (gameDto.Status, nextStatus)
        {
            case (var breakStatus, not "Break") when breakStatus.StartsWith("Break "):
                var breakStatusTag = breakStatus["Break ".Length..];
                var nextStatusTag = breakStatusTag switch
                {
                    "PreDraft" => StatusTag.PreDraftTag,
                    "Draft" => StatusTag.DraftTag,
                    "MainCompetition" => StatusTag.MainCompetitionTag,
                    "Ended" => StatusTag.EndedTag,
                    _ => throw new Exception("Unknown next status")
                };
                return Domain.Game.Status.NewBreak(nextStatusTag);
            case ("PreDraft", _):
                var preDraftDto = gameDto.PreDraft;
                if (preDraftDto is null)
                {
                    throw new Exception("PreDraftDto is null even though GameDto is in pre-draft");
                }

                PreDraftStatus? preDraftStatus;
                switch (preDraftDto.Status, preDraftDto.Index)
                {
                    case ("Running", { } competitionIndex) when preDraftDto.CurrentCompetition is not null:
                        var competitionSettings = preDraftSettings.Competitions_[competitionIndex];
                        preDraftStatus = Domain.Game.PreDraftStatus.NewRunning(
                            PreDraftCompetitionIndexModule.create(competitionIndex).Value,
                            DomainCreateCompetition(gameDto, preDraftDto.CurrentCompetition, competitionSettings,
                                hill));
                        break;
                    case ({ } statusString, _) when statusString.StartsWith("Break "):
                        preDraftStatus = Domain.Game.PreDraftStatus.NewBreak(PreDraftCompetitionIndexModule
                            .create(int.Parse(statusString.Substring("Break ".Length)))
                            .Value);
                        break;
                    default:
                        throw new Exception("Unknown pre-draft status");
                }

                return Domain.Game.Status.NewPreDraft(preDraftStatus);
            case ("Draft", _):
                var draftDto = gameDto.Draft;
                if (draftDto is null)
                {
                    throw new Exception("DraftDto is null even though GameDto is in draft");
                }

                if (!draftDto.IsRunning)
                {
                    throw new Exception("DraftDto is not running even though GameDto is in draft");
                }

                var gameJumperIds = draftDto.PlayersOrder.Select(PlayerId.NewPlayerId);
                var picksMap = draftDto.Picks.ToDictionary(keySelector: playerPicksDto =>
                {
                    var gamePlayerId = PlayerId.NewPlayerId(playerPicksDto.GamePlayerId);
                    return gamePlayerId;
                }, elementSelector: playerPicksDto =>
                {
                    var pickedJumperIds = playerPicksDto.GameJumperIds.Select(JumperId.NewJumperId);
                    return ListModule.OfSeq(pickedJumperIds);
                });
                var picksMapFSharp =
                    MapModule.OfSeq(picksMap.Select(kv => new Tuple<PlayerId, FSharpList<JumperId>>(kv.Key, kv.Value)));

                FSharpList<PlayerId>? nextPlayerIds = null;
                if (gameDto.Settings.DraftOrderPolicy == "Random")
                {
                    if (draftDto.NextPlayersOrder is null)
                        throw new Exception(
                            "NextPlayersOrder is null even though GameDto is in draft and order policy is random");
                    nextPlayerIds =
                        ListModule.OfSeq(draftDto.NextPlayersOrder.Select(PlayerId.NewPlayerId));
                }

                var draftResult = Domain.Game.Draft.Restore(draftSettings, ListModule.OfSeq(gameJumperIds), jumperIds,
                    picksMapFSharp,
                    nextPlayerIds);
                if (draftResult.IsError)
                {
                    throw new Exception(draftResult.ErrorValue.ToString());
                }

                var draft = draftResult.ResultValue;
                return Status.NewDraft(draft);
            case ("MainCompetition", _):
                var mainCompetitionDto = gameDto.MainCompetition;
                if (mainCompetitionDto is null)
                {
                    throw new Exception("MainCompetitionDto is null even though GameDto is in main competition");
                }

                var mainCompetition =
                    DomainCreateCompetition(gameDto, mainCompetitionDto, mainCompetitionSettings, hill);
                return Status.NewMainCompetition(mainCompetition);
            case ("Ended", _):
                var rankingDto = gameDto.Ranking;
                if (rankingDto is null)
                {
                    throw new Exception("GameRankingDto is null even though GameDto is in ended");
                }

                var positionAndPointsMap = rankingDto.Records.ToDictionary(
                    keySelector: rankingRecord => PlayerId.NewPlayerId(rankingRecord.GamePlayerId), elementSelector:
                    rankingRecord => RankingModule.PointsModule.create(rankingRecord.Points).Value);

                var mapFSharp = MapModule.OfSeq(positionAndPointsMap.Select(kv =>
                    new Tuple<PlayerId, RankingModule.Points>(kv.Key, kv.Value)));
                var gameRanking = Domain.Game.Ranking.Create(mapFSharp);
                return Status.NewEnded(gameRanking);
            default:
                throw new Exception("Unknown game status: " + gameDto.Status);
                ;
        }
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
    CompetitionSettingsDto MainCompetition,
    int DraftTargetPicks,
    int DraftMaxPicks,
    string DraftUniqueJumpersPolicy,
    string DraftOrderPolicy,
    string DraftTimeoutPolicy,
    string DraftRankingPolicy,
    int CompetitionJumpIntervalMs);

public record CompetitionDto(
    Guid Id,
    string Status, // "NotStarted" | "RoundInProgress" | "Ended"
    int? NextJumpInMs,
    int? RoundIndex,
    List<CompetitionJumperDto> Jumpers,
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
    int Bib,
    double Total,
    int Rank,
    IReadOnlyList<CompetitionRoundResultDto> RoundResults
);

public sealed record CompetitionRoundResultDto(
    // Guid GameJumperId,
    Guid JumpResultId,
    Guid CompetitionJumperId,
    int RoundIndex,
    double Distance,
    double Points,
    IReadOnlyList<double>? Judges,
    double? JudgePoints,
    double? WindCompensation,
    double WindAverage,
    int Gate,
    double? GateCompensation,
    double? TotalCompensation
);

public sealed record CompetitionJumperDto(
    Guid Id

    // Guid GameJumperId,
    // Guid CompetitionJumperId,
    // string Name,
    // string Surname,
    // string CountryFisCode
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
    List<Guid> JumperIds,
    List<Guid> PlayersOrder,
    List<Guid>? NextPlayersOrder, // Only for `Random`
    List<PlayerPicksDto> Picks);
// List<Guid> AvailableGameJumpers);

public record PlayerDto(Guid Id, string Nick);

// public record JumperDto(Guid GameJumperId, Guid CompetitionJumperId, Guid GameWorldJumperId);

public record JumperDto(Guid GameJumperId);

public record CompetitionHillDto(
    Guid Id,
    // string FisCountryCode,
    double KPoint,
    double HsPoint,
    double GatePoints,
    double HeadwindPoints,
    double TailwindPoints
);

public record GameRankingRecordDto(
    Guid GamePlayerId,
    int Rank,
    int Points
);

public record GameRankingDto(
    List<GameRankingRecordDto> Records);

public record GameDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    // string? NextStatus,
    SettingsDto Settings,
    CompetitionHillDto CompetitionHillDto,
    List<PlayerDto> Players,
    List<JumperDto> Jumpers,
    PreDraftDto? PreDraft,
    DraftDto? Draft,
    CompetitionDto? MainCompetition,
    EndedCompetitionDto? EndedMainCompetition,
    GameRankingDto? Ranking);
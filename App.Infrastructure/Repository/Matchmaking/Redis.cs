using System.Text.Json;
using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Matchmaking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using StackExchange.Redis;

namespace App.Infrastructure.Repository.Matchmaking;

public record SettingsDto(TimeSpan MaxDuration, string MatchmakingEndPolicy, int Min, int Max);

public record PlayerDto(Guid Id, string Nick, DateTimeOffset JoinedAt);

public record MatchmakingDto(
    Guid Id,
    string Status,
    SettingsDto Settings,
    List<PlayerDto> Players,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset? MaxReachedAt,
    DateTimeOffset? MinReachedAt,
    DateTimeOffset? LastUpdatedAt);

public static class MatchmakingDtoMapper
{
    public static MatchmakingDto ToRedis(Domain.Matchmaking.Matchmaking matchmaking)
    {
        var players = matchmaking.Players_
            .Select(player =>
                new PlayerDto(player.Id.Item, PlayerModule.NickModule.value(player.Nick), player.JoinedAt)).ToList();

        var matchmakingEndPolicy = RedisMatchmakingEndPolicy(matchmaking.EndPolicy);

        var settings = new SettingsDto(matchmaking.MaxDuration, matchmakingEndPolicy,
            SettingsModule.MinPlayersModule.value(matchmaking.MinPlayersCount),
            SettingsModule.MaxPlayersModule.value(matchmaking.MaxPlayersCount));
        return new MatchmakingDto(matchmaking.Id_.Item, matchmaking.Status_.FormattedStatus(), settings, players,
            matchmaking.StartedAt_, matchmaking.EndedAt_.ToNullable(), matchmaking.ReachedMaxPlayersAt_.ToNullable(),
            matchmaking.ReachedMinPlayersAt_.ToNullable(), matchmaking.LastUpdatedAt_.ToNullable());
    }

    private static string RedisMatchmakingEndPolicy(
        Domain.Matchmaking.SettingsModule.MatchmakingEndPolicy matchmakingEndPolicy)
    {
        return matchmakingEndPolicy switch
        {
            Domain.Matchmaking.SettingsModule.MatchmakingEndPolicy.AfterNoUpdate afterNoUpdate => $"AfterNoUpdate {
                afterNoUpdate.Since.ToString()}",
            Domain.Matchmaking.SettingsModule.MatchmakingEndPolicy.AfterReachingMaxPlayers afterReachingMaxPlayers =>
                $"AfterReachingMaxPlayers {afterReachingMaxPlayers.After.ToString()}",
            Domain.Matchmaking.SettingsModule.MatchmakingEndPolicy.AfterReachingMinPlayers afterReachingMinPlayers =>
                $"AfterReachingMinPlayers {afterReachingMinPlayers.After.ToString()}",
            { IsAfterTimeout: true } => "AfterTimeout",
            _ => throw new ArgumentOutOfRangeException(nameof(matchmakingEndPolicy), matchmakingEndPolicy, null)
        };
    }

    public static Domain.Matchmaking.Matchmaking ToDomain(this MatchmakingDto dto)
    {
        var matchmakingEndPolicy = DomainMatchmakingEndPolicy(dto.Settings.MatchmakingEndPolicy);

        var settings = Domain.Matchmaking.Settings.Create(SettingsModule.Duration.NewDuration(dto.Settings.MaxDuration),
            matchmakingEndPolicy,
            SettingsModule.MinPlayersModule.create(dto.Settings.Min).Value,
            SettingsModule.MaxPlayersModule.create(dto.Settings.Max).Value);
        if (settings.IsError)
        {
            throw new InvalidOperationException("Failed to create settings: " + settings.ErrorValue);
        }

        var status = MatchmakingExtensions.CreateDomainStatus(dto.Status);

        var players = dto.Players.Select(player =>
        {
            var nick = PlayerModule.NickModule.createWithSuffix(player.Nick);
            if (nick.IsNone())
            {
                throw new InvalidOperationException("Failed to create nick: " + player.Nick);
            }

            return new Domain.Matchmaking.Player(PlayerId.NewPlayerId(player.Id),
                nick.Value, player.JoinedAt);
        }).ToList();
        var matchmaking = Domain.Matchmaking.Matchmaking.CreateFromState(MatchmakingId.NewMatchmakingId(dto.Id),
            settings.ResultValue, status, SetModule.OfSeq(players), dto.StartedAt, dto.EndedAt, dto.MaxReachedAt,
            dto.MinReachedAt, dto.LastUpdatedAt);
        return matchmaking;
    }

    private static Domain.Matchmaking.SettingsModule.MatchmakingEndPolicy DomainMatchmakingEndPolicy(
        string matchmakingEndPolicy)
    {
        var parts = matchmakingEndPolicy.Split(' ');
        return parts[0] switch
        {
            "AfterNoUpdate" => SettingsModule.MatchmakingEndPolicy.NewAfterNoUpdate(
                TimeSpan.Parse(parts[1])),
            "AfterReachingMaxPlayers" => SettingsModule.MatchmakingEndPolicy.NewAfterReachingMaxPlayers(
                TimeSpan.Parse(parts[1])),
            "AfterReachingMinPlayers" => SettingsModule.MatchmakingEndPolicy.NewAfterReachingMinPlayers(
                TimeSpan.Parse(parts[1])),
            "AfterTimeout" => SettingsModule.MatchmakingEndPolicy.AfterTimeout,
            _ => throw new ArgumentOutOfRangeException(nameof(matchmakingEndPolicy), matchmakingEndPolicy, null)
        };
    }
}

public class Redis(IConnectionMultiplexer redis, IMyLogger logger) : IMatchmakings
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => "matchmaking:live";
    private static string ArchivePattern => "matchmaking:archive";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";
    private static string LiveSetKey => $"{LivePattern}:ids";
    private static string ArchiveSetKey => $"{ArchivePattern}:ids";

    private async Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetMatchmakingsFromSet(
        string cacheKey,
        string setKey,
        Func<Guid, string> keySelector,
        Func<MatchmakingDto, bool> filter,
        TimeSpan ttl,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Domain.Matchmaking.Matchmaking>? cached))
            return cached!;

        var ids = await _db.SetMembersAsync(setKey);
        if (ids.Length == 0)
            return [];

        var keys = ids.Select(id => (RedisKey)keySelector(Guid.Parse(id.ToString()))).ToArray();
        var values = await _db.StringGetAsync(keys);

        var result = new List<Domain.Matchmaking.Matchmaking>();
        var toRemove = new List<RedisValue>();

        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            var json = values[i];

            if (!json.HasValue)
            {
                toRemove.Add(id);
                continue;
            }

            var dto = JsonSerializer.Deserialize<MatchmakingDto>(json!);
            if (dto is null)
            {
                toRemove.Add(id);
                continue;
            }

            if (filter(dto))
                result.Add(dto.ToDomain());
        }

        if (toRemove.Count > 0)
            await _db.SetRemoveAsync(setKey, toRemove.ToArray());

        _cache.Set(cacheKey, result, ttl);
        return result;
    }

    public async Task Add(Domain.Matchmaking.Matchmaking matchmaking, CancellationToken ct)
    {
        try
        {
            var id = matchmaking.Id_.Item;
            var dto = MatchmakingDtoMapper.ToRedis(matchmaking);
            var json = JsonSerializer.Serialize(dto);
            var idStr = dto.Id.ToString();

            if (matchmaking.Status_.IsRunning)
            {
                await Task.WhenAll(
                    _db.StringSetAsync(LiveKey(id), json, TimeSpan.FromSeconds(120)),
                    _db.SetAddAsync(LiveSetKey, idStr)
                );
            }
            else
            {
                var tran = _db.CreateTransaction();
                tran.StringSetAsync(ArchiveKey(id), json);
                tran.SetAddAsync(ArchiveSetKey, idStr);
                await RemoveLiveMatchmaking(id, tran, ct);
                var ok = await tran.ExecuteAsync();
                if (!ok)
                    logger.Warn("Redis Matchmakings.Add: transaction failed");
            }

            // czyścimy cache po zmianie
            _cache.Remove("GetInProgress");
            _cache.Remove("GetEnded");
        }
        catch (ObjectDisposedException ex)
        {
            logger.Warn($"Redis disposed during shutdown. Skipping Add(): {ex.Message}");
        }
    }

    private async Task RemoveLiveMatchmaking(Guid id, ITransaction? tran, CancellationToken ct)
    {
        if (tran is not null)
        {
            tran.KeyDeleteAsync(LiveKey(id));
            tran.SetRemoveAsync(LiveSetKey, id.ToString());
        }
        else
        {
            await _db.KeyDeleteAsync(LiveKey(id));
            await _db.SetRemoveAsync(LiveSetKey, id.ToString());
        }
    }

    // ------------------- QUERIES -------------------

    public async Task<FSharpOption<Domain.Matchmaking.Matchmaking>> GetById(MatchmakingId id, CancellationToken ct)
    {
        var cacheKey = $"matchmaking:{id.Item}";
        if (_cache.TryGetValue(cacheKey, out FSharpOption<Domain.Matchmaking.Matchmaking>? cached))
            return cached!;

        var values = await _db.StringGetAsync([LiveKey(id.Item), ArchiveKey(id.Item)]);
        var json = values.FirstOrDefault(v => v.HasValue);
        if (!json.HasValue)
        {
            _cache.Set(cacheKey, FSharpOption<Domain.Matchmaking.Matchmaking>.None, TimeSpan.FromSeconds(3));
            throw new KeyNotFoundException($"Matchmaking {id} not found");
        }

        var dto = JsonSerializer.Deserialize<MatchmakingDto>(json!) ?? throw new Exception("Failed to deserialize DTO");
        var domain = dto.ToDomain();
        _cache.Set(cacheKey, domain, TimeSpan.FromSeconds(1000));
        return domain;
    }

    public Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetInProgress(CancellationToken ct) =>
        GetMatchmakingsFromSet(
            "GetInProgress",
            LiveSetKey,
            LiveKey,
            dto => dto.Status == "Running" || dto.Status == "Waiting",
            TimeSpan.FromMilliseconds(1000),
            ct);

    public Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetEnded(CancellationToken ct) =>
        GetMatchmakingsFromSet(
            "GetEnded",
            ArchiveSetKey,
            ArchiveKey,
            dto => dto.Status != "Running" && dto.Status != "Waiting",
            TimeSpan.FromSeconds(1000),
            ct);
}
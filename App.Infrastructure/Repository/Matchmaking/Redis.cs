using System.Text.Json;
using App.Application.Extensions;
using App.Application.Utility;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using StackExchange.Redis;

namespace App.Infrastructure.Repository.Matchmaking;

public record SettingsDto(TimeSpan MaxDuration, string MatchmakingEndPolicy, int Min, int Max);

public record PlayerDto(Guid Id, string Nick);

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
            .Select(player => new PlayerDto(player.Id.Item, PlayerModule.NickModule.value(player.Nick))).ToList();

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
                nick.Value);
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
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"matchmaking:live";
    private static string LiveKey(Guid id) => $"matchmaking:live:{id}";
    private static string ArchivePattern => $"matchmaking:archive";
    private static string ArchiveKey(Guid id) => $"matchmaking:archive:{id}";
    private static string LiveSetKey => "matchmaking:live:ids";
    private static string ArchiveSetKey => "matchmaking:archive:ids";

    public async Task Add(Domain.Matchmaking.Matchmaking matchmaking, CancellationToken ct)
    {
        var matchmakingId = matchmaking.Id_.Item;
        var dto = MatchmakingDtoMapper.ToRedis(matchmaking);
        var serializedMatchmaking = JsonSerializer.Serialize(dto);
        if (matchmaking.Status_.IsRunning)
        {
            await _db.StringSetAsync(LiveKey(matchmakingId), serializedMatchmaking, TimeSpan.FromSeconds(120));
            await _db.SetAddAsync(LiveSetKey, dto.Id.ToString());
        }
        else
        {
            var transaction = _db.CreateTransaction();
            transaction.StringSetAsync(ArchiveKey(matchmakingId), serializedMatchmaking);
            transaction.SetAddAsync(ArchiveSetKey, dto.Id.ToString());
            await RemoveLiveMatchmaking(matchmakingId, transaction, ct);
            var executed = await transaction.ExecuteAsync();
            if (!executed)
                logger.Warn("Redis IMatchmakings.Add: Transaction failed");
        }
    }

    private async Task RemoveLiveMatchmaking(Guid matchmakingId, ITransaction? transaction, CancellationToken ct)
    {
        if (transaction is not null)
        {
            transaction.KeyDeleteAsync(LiveKey(matchmakingId));
            transaction.SetRemoveAsync(LiveSetKey, matchmakingId.ToString());
        }
        else
        {
            await _db.KeyDeleteAsync(LiveKey(matchmakingId));
            await _db.SetRemoveAsync(LiveSetKey, matchmakingId.ToString());
        }
    }

    public async Task<FSharpOption<Domain.Matchmaking.Matchmaking>> GetById(MatchmakingId matchmakingId,
        CancellationToken ct)
    {
        var liveMatchmakingJson = await _db.StringGetAsync(LiveKey(matchmakingId.Item));
        if (liveMatchmakingJson.HasValue)
        {
            var dto = JsonSerializer.Deserialize<MatchmakingDto>(liveMatchmakingJson!);
            if (dto is null)
            {
                throw new Exception("Failed to deserialize live matchmaking");
            }

            return dto.ToDomain();
        }

        var archivedMatchmakingJson = await _db.StringGetAsync(ArchiveKey(matchmakingId.Item));
        if (archivedMatchmakingJson.HasValue)
        {
            var dto = JsonSerializer.Deserialize<MatchmakingDto>(archivedMatchmakingJson!);
            if (dto is null)
            {
                throw new Exception("Failed to deserialize archived matchmaking");
            }

            return dto.ToDomain();
        }

        throw new KeyNotFoundException($"Matchmaking {matchmakingId} not found");
    }

    public async Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetInProgress(CancellationToken ct)
    {
        var ids = await _db.SetMembersAsync("matchmaking:live:ids");
        var matchmakings = new List<Domain.Matchmaking.Matchmaking>();
        foreach (var id in ids)
        {
            var json = await _db.StringGetAsync($"matchmaking:live:{id}");
            if (!json.HasValue)
            {
                await _db.SetRemoveAsync(LiveSetKey, id.ToString());
                continue;
            }

            var dto = JsonSerializer.Deserialize<MatchmakingDto>(json!);
            if (dto != null)
                matchmakings.Add(dto.ToDomain());
        }

        return matchmakings.Where(matchmaking => matchmaking.Status_.IsRunning);
    }

    public async Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetEnded(CancellationToken ct)
    {
        var ids = await _db.SetMembersAsync("matchmaking:archive:ids");
        var matchmakings = new List<Domain.Matchmaking.Matchmaking>();
        foreach (var id in ids)
        {
            var json = await _db.StringGetAsync($"matchmaking:archive:{id}");
            if (!json.HasValue)
            {
                await _db.SetRemoveAsync(LiveSetKey, id.ToString());
                continue;
            }

            var dto = JsonSerializer.Deserialize<MatchmakingDto>(json!);
            if (dto != null)
                matchmakings.Add(dto.ToDomain());
        }

        return matchmakings.Where(matchmaking => !matchmaking.Status_.IsRunning);
    }
}
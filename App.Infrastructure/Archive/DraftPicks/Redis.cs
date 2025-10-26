using System.Text.Json;
using App.Application.Game.DraftPicks;
using App.Application.Utility;
using App.Domain.Game;
using RedisRepository = App.Infrastructure.Repository.Game;
using StackExchange.Redis;

namespace App.Infrastructure.Archive.DraftPicks;

/// <summary>
/// Uses redis values shared with IGames Redis implementation
/// </summary>
public class Redis(IConnectionMultiplexer redis, IMyLogger logger) : IDraftPicksArchive
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static string LivePattern => $"game:live";
    private static string LiveKey(Guid id) => $"{LivePattern}:{id}";
    private static string ArchivePattern => $"game:archive";
    private static string ArchiveKey(Guid id) => $"{ArchivePattern}:{id}";

    public async Task Archive(Guid gameId, Dictionary<PlayerId, IEnumerable<JumperId>> picks)
    {
        var dto = await GetGameDto(gameId, searchInArchive: false);

        if (dto.Draft is null)
            throw new Exception($"DraftDto is null (status={dto.Status}");

        var picksList = picks
            .Select(p => new RedisRepository.PlayerPicksDto(p.Key.Item, p.Value.Select(j => j.Item).ToList()))
            .ToList();

        var newGame = dto with { Draft = dto.Draft with { Picks = picksList } };
        await _db.StringSetAsync(LiveKey(gameId), JsonSerializer.Serialize(newGame));
    }

    public async Task<Dictionary<PlayerId, IEnumerable<JumperId>>?> GetPicks(Guid gameId)
    {
        try
        {
            var dto = await GetGameDto(gameId, searchInArchive: true);

            if (dto.Draft is not null)
                return dto.Draft.Picks.ToDictionary(
                    pick => PlayerId.NewPlayerId(pick.GamePlayerId),
                    pick => pick.GameJumperIds.Select(JumperId.NewJumperId)
                );

            logger.Warn($"DraftDto is null (status={dto.Status})");
            return null;
        }
        catch (GameNotFoundException)
        {
            return null;
        }
        catch (RedisTimeoutException ex)
        {
            logger.Warn($"Timeout while getting draft picks for game {gameId}: {ex.Message}");
            return null;
        }
        catch (RedisConnectionException ex)
        {
            logger.Warn($"Redis connection issue while getting draft picks for game {gameId}: {ex.Message}");
            return null;
        }
    }

    private async Task<RedisRepository.GameDto> GetGameDto(Guid gameId, bool searchInArchive)
    {
        // Prefer replica for reads to reduce load; Upstash handles routing
        var liveJson = await _db.StringGetAsync(LiveKey(gameId), CommandFlags.PreferReplica);
        if (liveJson.HasValue)
            return Deserialize(liveJson);

        if (!searchInArchive) throw new GameNotFoundException();

        var archiveJson = await _db.StringGetAsync(ArchiveKey(gameId), CommandFlags.PreferReplica);
        if (archiveJson.HasValue)
            return Deserialize(archiveJson);

        throw new GameNotFoundException();

        static RedisRepository.GameDto Deserialize(RedisValue json) =>
            JsonSerializer.Deserialize<RedisRepository.GameDto>(json!)
            ?? throw new Exception("Failed to deserialize game JSON");
    }
}

public class GameNotFoundException(string? message = null) : Exception(message);
using System.Collections.Concurrent;

namespace App.Web.Security;

public class InMemoryGamePlayerMappingStore : IGamePlayerMappingStore
{
    private readonly ConcurrentDictionary<Guid, (Guid MatchmakingId, IReadOnlyDictionary<Guid, Guid> Map)> _byGame = new();

    public void Store(Guid matchmakingId, Guid gameId, IReadOnlyDictionary<Guid, Guid> matchmakingToGame)
    {
        _byGame[gameId] = (matchmakingId, matchmakingToGame);
    }

    public bool TryGetByGame(Guid gameId, out Guid matchmakingId, out IReadOnlyDictionary<Guid, Guid> matchmakingToGame)
    {
        if (_byGame.TryGetValue(gameId, out var tuple))
        {
            matchmakingId = tuple.MatchmakingId;
            matchmakingToGame = tuple.Map;
            return true;
        }
        matchmakingId = Guid.Empty;
        matchmakingToGame = new Dictionary<Guid, Guid>();
        return false;
    }

    public void Remove(Guid gameId)
    {
        _byGame.TryRemove(gameId, out _);
    }
}

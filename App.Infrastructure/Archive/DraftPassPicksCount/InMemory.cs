namespace App.Infrastructure.Archive.DraftPassPicksCount;

using App.Application.Game.PassPicksCount;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InMemory : IDraftPassPicksCountArchive
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, int>> _store = new();

    public Task<int> Get(Guid gameId, Guid playerId)
    {
        if (_store.TryGetValue(gameId, out var playerCounts) &&
            playerCounts.TryGetValue(playerId, out var count))
            return Task.FromResult(count);

        return Task.FromResult(0);
    }

    public Task<Dictionary<Guid, int>> GetDictionary(Guid gameId)
    {
        return Task.FromResult(_store.TryGetValue(gameId, out var playerCounts)
            ? new Dictionary<Guid, int>(playerCounts)
            : new Dictionary<Guid, int>());
    }

    public Task Add(Guid gameId, Guid playerId)
    {
        var players = _store.GetOrAdd(gameId, _ => new ConcurrentDictionary<Guid, int>());
        players.AddOrUpdate(playerId, 1, (_, old) => old + 1);
        return Task.CompletedTask;
    }
}
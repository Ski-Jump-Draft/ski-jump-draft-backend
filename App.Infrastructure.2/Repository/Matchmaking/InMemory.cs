using System.Collections.Concurrent;
using App.Domain._2.Matchmaking;

namespace App.Infrastructure._2.Repository.Matchmaking;

public class InMemory : IMatchmakings
{
    private readonly ConcurrentDictionary<Guid, Domain._2.Matchmaking.Matchmaking> _store = new();

    public Task Add(Domain._2.Matchmaking.Matchmaking matchmaking, CancellationToken ct)
    {
        _store[matchmaking.Id_.Item] = matchmaking;
        return Task.CompletedTask;
    }

    public Task<Domain._2.Matchmaking.Matchmaking> GetById(MatchmakingId matchmakingId, CancellationToken ct)
    {
        if (_store.TryGetValue(matchmakingId.Item, out var mm))
            return Task.FromResult(mm);

        throw new KeyNotFoundException($"Matchmaking {matchmakingId} not found");
    }

    public Task<IEnumerable<Domain._2.Matchmaking.Matchmaking>> GetInProgress(CancellationToken ct)
    {
        var result = _store.Values.Where(mm => mm.Status_.Equals(Status.Running));
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<IEnumerable<Domain._2.Matchmaking.Matchmaking>> GetEnded(CancellationToken ct)
    {
        var result = _store.Values.Where(mm =>
            mm.Status_ is Status.Ended or Status.Failed);
        return Task.FromResult(result.AsEnumerable());
    }
}
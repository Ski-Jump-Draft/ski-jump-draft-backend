using System.Collections.Concurrent;
using App.Application.Extensions;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.Repository.Matchmaking;

public class InMemory : IMatchmakings
{
    private readonly ConcurrentDictionary<Guid, Domain.Matchmaking.Matchmaking> _store = new();

    public Task Add(Domain.Matchmaking.Matchmaking matchmaking, CancellationToken ct)
    {
        _store[matchmaking.Id_.Item] = matchmaking;
        return Task.CompletedTask;
    }

    public Task<FSharpOption<Domain.Matchmaking.Matchmaking>> GetById(MatchmakingId matchmakingId, CancellationToken ct)
    {
        if (_store.TryGetValue(matchmakingId.Item, out var mm))
            return Task.FromResult(FSharpOption<Domain.Matchmaking.Matchmaking>.Some(mm));

        throw new KeyNotFoundException($"Matchmaking {matchmakingId} not found");
    }

    public Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetInProgress(FSharpOption<MatchmakingType> type,
        CancellationToken ct)
    {
        var result =
            _store.Values.Where(mm =>
                mm.Status_.Equals(Status.Running) && MatchmakingIsEligible(mm, type.ToNullable()));
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<IEnumerable<Domain.Matchmaking.Matchmaking>> GetEnded(FSharpOption<MatchmakingType> type,
        CancellationToken ct)
    {
        var result = _store.Values.Where(mm =>
            mm.Status_ is Status.Ended or Status.Failed && MatchmakingIsEligible(mm, type.ToNullable()));
        return Task.FromResult(result.AsEnumerable());
    }

    private bool MatchmakingIsEligible(Domain.Matchmaking.Matchmaking matchmaking, MatchmakingType? type)
    {
        if (type is null) return true;
        if (type.IsPremium) return matchmaking.IsPremium_;
        return !matchmaking.IsPremium_;
    }
}
using System.Collections.Concurrent;
using App.Domain._2.Game;
using Microsoft.FSharp.Core;

namespace App.Infrastructure._2.Repository.Game;

public class InMemory : IGames
{
    private readonly ConcurrentDictionary<Guid, Domain._2.Game.Game> _store = new();

    public Task Add(Domain._2.Game.Game game, CancellationToken ct)
    {
        _store[game.Id_.Item] = game;
        return Task.CompletedTask;
    }

    public Task<FSharpOption<Domain._2.Game.Game>> GetById(GameId gameId, CancellationToken ct)
    {
        if (_store.TryGetValue(gameId.Item, out var game))
            return Task.FromResult(FSharpOption<Domain._2.Game.Game>.Some(game));

        throw new KeyNotFoundException($"Game {gameId} not found");
    }

    public Task<IEnumerable<Domain._2.Game.Game>> GetNotStarted(CancellationToken ct)
    {
        var result = _store.Values.Where(game => !game.StatusTag.Equals(StatusTag.NewBreakTag(StatusTag.PreDraftTag)));
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<IEnumerable<Domain._2.Game.Game>> GetInProgress(CancellationToken ct)
    {
        var result = _store.Values.Where(game => !game.StatusTag.Equals(StatusTag.EndedTag));
        return Task.FromResult(result.AsEnumerable());
    }

    public async Task<int> GetInProgressCount(CancellationToken ct)
    {
        return (await GetInProgress(ct)).Count();
    }

    public Task<IEnumerable<Domain._2.Game.Game>> GetEnded(CancellationToken ct)
    {
        var result = _store.Values.Where(game => game.StatusTag.Equals(StatusTag.EndedTag));
        return Task.FromResult(result.AsEnumerable());
    }
}
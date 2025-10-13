using System.Collections.Concurrent;
using App.Application.Matchmaking;

namespace App.Infrastructure.PremiumMatchmakings;

public class InMemory : IPremiumMatchmakings
{
    private readonly ConcurrentDictionary<string, Guid> _byPassword = new();
    private readonly ConcurrentDictionary<Guid, string> _byId = new();
    private readonly ConcurrentDictionary<string, Guid> _gameByPassword = new();

    public Task<Guid> Add(string password, Guid matchmakingId)
    {
        if (_byId.TryGetValue(matchmakingId, out var oldPassword) && oldPassword != password)
        {
            _byPassword.TryRemove(oldPassword, out _);
            _gameByPassword.TryRemove(oldPassword, out _);
        }

        _byPassword[password] = matchmakingId;
        _byId[matchmakingId] = password;

        _gameByPassword.TryRemove(password, out _);

        return Task.FromResult(matchmakingId);
    }

    public Task<bool> StartGameIfBelongsToMatchmaking(Guid matchmakingId, Guid gameId)
    {
        if (!_byId.TryGetValue(matchmakingId, out var pwd)) return Task.FromResult(false);
        _gameByPassword[pwd] = gameId;
        return Task.FromResult(true);
    }

    public Task<bool> EndGameIfRuns(Guid gameId)
    {
        var kvp = _gameByPassword.FirstOrDefault(x => x.Value == gameId);
        if (kvp.Key is null) return Task.FromResult(false);
        _gameByPassword.TryRemove(kvp.Key, out _);
        return Task.FromResult(true);
    }
    //
    // public Task<bool> GameRunsByPremiumMatchmaking(string password)
    // {
    //     var runs = _gameByPassword.ContainsKey(password);
    //     return Task.FromResult(runs);
    // }

    public Task<bool> GameRunsByPremiumMatchmaking(string password)
    {
        var runs = _gameByPassword.ContainsKey(password);

        var byPwdDump = string.Join(", ",
            _byPassword.Select(kvp => $"[{kvp.Key} → {kvp.Value}]"));
        var byIdDump = string.Join(", ",
            _byId.Select(kvp => $"[{kvp.Key} → {kvp.Value}]"));
        var gamesDump = string.Join(", ",
            _gameByPassword.Select(kvp => $"[{kvp.Key} → {kvp.Value}]"));

        Console.WriteLine("=== PremiumMatchmakings State Dump ===");
        Console.WriteLine($"Password check: {password}");
        Console.WriteLine($"_byPassword: {byPwdDump}");
        Console.WriteLine($"_byId: {byIdDump}");
        Console.WriteLine($"_gameByPassword: {gamesDump}");
        Console.WriteLine($"Runs for this password? {runs}");
        Console.WriteLine("======================================");

        return Task.FromResult(runs);
    }

    public Task<bool> GameRunsByPremiumMatchmaking(Guid matchmakingId)
    {
        return Task.FromResult(false);
    }


    public Task Remove(string password)
    {
        if (_byPassword.TryRemove(password, out var id))
            _byId.TryRemove(id, out _);

        _gameByPassword.TryRemove(password, out _);
        return Task.CompletedTask;
    }

    public Task Remove(Guid matchmakingId)
    {
        if (!_byId.TryRemove(matchmakingId, out var pwd)) return Task.CompletedTask;
        _byPassword.TryRemove(pwd, out _);
        _gameByPassword.TryRemove(pwd, out _);

        return Task.CompletedTask;
    }

    public Task<string?> GetPassword(Guid matchmakingId)
    {
        return Task.FromResult(_byId.TryGetValue(matchmakingId, out var pwd) ? pwd : null as string);
    }

    public Task<Guid?> GetPremiumMatchmakingId(string password)
    {
        return Task.FromResult(_byPassword.TryGetValue(password, out var id) ? id as Guid? : null);
    }

    public Task<bool> PremiumMatchmakingIsRunning(Guid matchmakingId)
    {
        var exists = _byId.ContainsKey(matchmakingId);
        return Task.FromResult(exists);
    }
}
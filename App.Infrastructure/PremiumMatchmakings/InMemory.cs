using System.Collections.Concurrent;
using App.Application.Matchmaking;

namespace App.Infrastructure.PremiumMatchmakings;

public class InMemory : IPremiumMatchmakingGames
{
    private readonly ConcurrentDictionary<string, Guid> _idByPassword = new();
    private readonly ConcurrentDictionary<Guid, string> _passwordById = new();
    private readonly ConcurrentDictionary<string, Guid> _gameByPassword = new();
    private readonly ConcurrentDictionary<Guid, bool> _ended = new();

    public Task<Guid> Add(string password, Guid matchmakingId)
    {
        _ended.TryRemove(matchmakingId, out _); // restart, jeśli kiedyś zakończony

        if (_passwordById.TryGetValue(matchmakingId, out var oldPassword) && oldPassword != password)
        {
            _idByPassword.TryRemove(oldPassword, out _);
            _gameByPassword.TryRemove(oldPassword, out _);
        }

        _idByPassword[password] = matchmakingId;
        _passwordById[matchmakingId] = password;
        _gameByPassword.TryRemove(password, out _);

        return Task.FromResult(matchmakingId);
    }

    public Task<bool> StartGameIfBelongsToMatchmaking(Guid matchmakingId, Guid gameId)
    {
        if (!_passwordById.TryGetValue(matchmakingId, out var password))
            return Task.FromResult(false);

        _gameByPassword[password] = gameId;
        return Task.FromResult(true);
    }

    public Task<int> GetGamesCount() => Task.FromResult(_gameByPassword.Count);

    public Task<bool> EndGameIfRuns(Guid gameId)
    {
        var kvp = _gameByPassword.FirstOrDefault(x => x.Value == gameId);
        if (kvp.Key is null) return Task.FromResult(false);
        _gameByPassword.TryRemove(kvp.Key, out _);
        return Task.FromResult(true);
    }

    public Task<bool> GameRunsByPremiumMatchmaking(string password)
    {
        var runs = _gameByPassword.ContainsKey(password);
        return Task.FromResult(runs);
    }

    public Task<bool> GameRunsByPremiumMatchmaking(Guid matchmakingId)
    {
        if (!_passwordById.TryGetValue(matchmakingId, out var pwd)) return Task.FromResult(false);
        return Task.FromResult(_gameByPassword.ContainsKey(pwd));
    }

    public Task EndMatchmaking(Guid matchmakingId)
    {
        _ended[matchmakingId] = true; // oznacz jako zakończony
        return Task.CompletedTask;
    }

    public Task<string?> GetPassword(Guid matchmakingId)
    {
        if (_ended.ContainsKey(matchmakingId)) return Task.FromResult<string?>(null);
        return Task.FromResult(_passwordById.TryGetValue(matchmakingId, out var pwd) ? pwd : null);
    }

    public Task<Guid?> GetPremiumMatchmakingId(string password)
    {
        if (!_idByPassword.TryGetValue(password, out var id)) return Task.FromResult<Guid?>(null);
        if (_ended.ContainsKey(id)) return Task.FromResult<Guid?>(null);
        return Task.FromResult<Guid?>(id);
    }

    public Task<bool> PremiumMatchmakingIsRunning(Guid matchmakingId)
    {
        if (_ended.ContainsKey(matchmakingId)) return Task.FromResult(false);
        if (!_passwordById.TryGetValue(matchmakingId, out var pwd)) return Task.FromResult(false);

        var active = _gameByPassword.ContainsKey(pwd);
        return Task.FromResult(active);
    }
}

using System.Collections.Concurrent;
using App.Application.Game.GameWind;
using App.Domain.Simulation;

namespace App.Infrastructure.GameWind;

public class InMemory : IGameWind
{
    private readonly ConcurrentDictionary<Guid, Wind> _storage = new();

    public Wind? Get(Guid gameId)
        => _storage.GetValueOrDefault(gameId);

    public void Set(Guid gameId, Wind value)
        => _storage[gameId] = value;
}
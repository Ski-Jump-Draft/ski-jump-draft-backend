using App.Application.Bot;

namespace App.Infrastructure.Bot.PassPickLock;

public class InMemory : IBotPickLock
{
    private readonly HashSet<(Guid GameId, Guid PlayerId)> _locks = new();
    private readonly object _sync = new();

    public void Lock(Guid gameId, Guid playerId)
    {
        lock (_sync)
        {
            _locks.Add((gameId, playerId));
        }
    }

    public void Unlock(Guid gameId, Guid playerId)
    {
        lock (_sync)
        {
            _locks.Remove((gameId, playerId));
        }
    }

    public bool IsLocked(Guid gameId, Guid playerId)
    {
        lock (_sync)
        {
            return _locks.Contains((gameId, playerId));
        }
    }
}

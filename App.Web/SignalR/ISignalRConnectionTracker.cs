using System.Collections.Concurrent;

namespace App.Web.SignalR;

public interface ISignalRConnectionTracker
{
    int Increment(Guid matchmakingId, Guid playerId);
    int Decrement(Guid matchmakingId, Guid playerId);
    int GetCount(Guid matchmakingId, Guid playerId);
}

public class InMemorySignalRConnectionTracker : ISignalRConnectionTracker
{
    private readonly ConcurrentDictionary<(Guid mmId, Guid playerId), int> _counts = new();

    public int Increment(Guid matchmakingId, Guid playerId)
    {
        return _counts.AddOrUpdate((matchmakingId, playerId), 1, (_, old) => old + 1);
    }

    public int Decrement(Guid matchmakingId, Guid playerId)
    {
        var key = (matchmakingId, playerId);
        while (true)
        {
            if (_counts.TryGetValue(key, out var old))
            {
                var next = Math.Max(0, old - 1);
                if (next == 0)
                {
                    _counts.TryRemove(key, out _);
                    return 0;
                }
                if (_counts.TryUpdate(key, next, old))
                    return next;
                // retry CAS
                continue;
            }
            return 0;
        }
    }

    public int GetCount(Guid matchmakingId, Guid playerId)
        => _counts.TryGetValue((matchmakingId, playerId), out var val) ? val : 0;
}

using System.Collections.Concurrent;

namespace App.Web.Sse;

public interface ISseConnectionTracker
{
    // Increment active connections for (mmId, playerId) and return new count
    int Increment(Guid matchmakingId, Guid playerId);
    // Decrement active connections for (mmId, playerId) and return new count (never below 0)
    int Decrement(Guid matchmakingId, Guid playerId);
    // Get current active connections count for (mmId, playerId)
    int GetCount(Guid matchmakingId, Guid playerId);
}

public class InMemorySseConnectionTracker : ISseConnectionTracker
{
    private readonly ConcurrentDictionary<(Guid mmId, Guid playerId), int> _counts = new();

    public int Increment(Guid matchmakingId, Guid playerId)
    {
        return _counts.AddOrUpdate((matchmakingId, playerId), 1, (_, old) => old + 1);
    }

    public int Decrement(Guid matchmakingId, Guid playerId)
    {
        var key = (matchmakingId, playerId);
        int newVal;
        _counts.AddOrUpdate(key, 0, (_, old) =>
        {
            var nv = old - 1;
            newVal = nv < 0 ? 0 : nv;
            return newVal;
        });
        // The above doesn't allow reading newVal outside; recompute safely
        while (true)
        {
            if (_counts.TryGetValue(key, out var val))
            {
                if (val <= 0)
                {
                    _counts.TryRemove(key, out _);
                    return 0;
                }
                return val;
            }
            return 0;
        }
    }

    public int GetCount(Guid matchmakingId, Guid playerId)
    {
        if (_counts.TryGetValue((matchmakingId, playerId), out var val))
            return val;
        return 0;
    }
}

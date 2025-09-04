using System.Collections.Concurrent;
using App.Application.Matchmaking;
using App.Application.Utility;

namespace App.Infrastructure.Matchmaking.Schedule;

public sealed class InMemory(IClock clock) : IMatchmakingSchedule
{
    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    private sealed record Entry(DateTimeOffset Start, TimeSpan Duration)
    {
        public DateTimeOffset End => Start + Duration;
    }

    public void StartMatchmaking(Guid matchmakingId, TimeSpan maxDuration)
    {
        var now = clock.Now();
        var entry = new Entry(now, maxDuration);
        _entries[matchmakingId] = entry;
    }

    public void EndMatchmaking(Guid matchmakingId)
    {
        _entries.Remove(matchmakingId, out _);
    }

    public TimeSpan GetRemainingTime(Guid matchmakingId)
    {
        if (!_entries.TryGetValue(matchmakingId, out var entry))
            throw new KeyNotFoundException($"No entry for matchmaking {matchmakingId}");

        return entry.End - clock.Now();
    }

    public bool ShouldEnd(Guid matchmakingId)
    {
        return GetRemainingTime(matchmakingId) <= TimeSpan.Zero;
    }
}
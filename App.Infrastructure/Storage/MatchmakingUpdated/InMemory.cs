using System.Collections.Concurrent;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;

namespace App.Infrastructure.Storage.MatchmakingUpdated;

public class InMemory : IMatchmakingUpdatedDtoStorage
{
    private readonly ConcurrentDictionary<Guid, MatchmakingUpdatedDto> _store = new();

    public Task<MatchmakingUpdatedDto?> Get(Guid matchmakingId)
    {
        _store.TryGetValue(matchmakingId, out var dto);
        return Task.FromResult(dto);
    }

    public Task Set(Guid matchmakingId, MatchmakingUpdatedDto dto)
    {
        Console.WriteLine("SetFirst!");
        _store.AddOrUpdate(matchmakingId, dto, (_, __) => dto);
        return Task.CompletedTask;
    }
}
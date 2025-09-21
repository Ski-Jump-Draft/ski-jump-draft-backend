using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using System.Collections.Concurrent;

namespace App.Infrastructure.Utility.GameUpdatedDto;

public class MapperInMemoryCache : IGameUpdatedDtoMapperCache
{
    private readonly ConcurrentDictionary<Guid, EndedPreDraftDto> _cache = new();

    public Task<EndedPreDraftDto?> GetEndedPreDraft(Guid gameId, CancellationToken ct = default)
    {
        _cache.TryGetValue(gameId, out var dto);
        return Task.FromResult(dto);
    }

    public Task SetEndedPreDraft(Guid gameId, EndedPreDraftDto preDraftDto, CancellationToken ct = default)
    {
        _cache[gameId] = preDraftDto;
        return Task.CompletedTask;
    }
}
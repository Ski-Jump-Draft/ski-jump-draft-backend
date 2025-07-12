using App.Application.Abstractions;
using App.Domain;
using App.Domain.Draft;
using App.Domain.Game;
using Id = App.Domain.Draft.Id;

namespace App.Infrastructure.DraftToGameMapStore;

public class InMemoryDraftToGameMapStore : IDraftToGameMapStore
{
    private readonly Dictionary<Id.Id, Domain.Game.Id.Id> _map = new();

    public Task<MapResult> TryGetGameIdByDraftIdAsync(Id.Id draftId, CancellationToken ct)
    {
        var found = _map.TryGetValue(draftId, out var gameId);
        return Task.FromResult(new MapResult(found, found ? gameId : null));
    }

    public Task AddMappingAsync(Id.Id draftId, Domain.Game.Id.Id gameId, CancellationToken ct)
    {
        _map[draftId] = gameId;
        return Task.CompletedTask;
    }
}
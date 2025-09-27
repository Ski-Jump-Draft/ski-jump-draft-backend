using App.Application.Game.DraftPicks;
using App.Domain.Game;

namespace App.Infrastructure.Archive.DraftPicks;

public class InMemory : IDraftPicksArchive
{
    private readonly Dictionary<Guid, Dictionary<PlayerId, IEnumerable<JumperId>>> _storage = new();

    public Task Archive(Guid gameId, Dictionary<PlayerId, IEnumerable<JumperId>> picks)
    {
        _storage[gameId] = picks;
        return Task.CompletedTask;
    }

    public Task<Dictionary<PlayerId, IEnumerable<JumperId>>> GetPicks(Guid gameId)
    {
        return Task.FromResult(_storage.TryGetValue(gameId, out var picks)
            ? new Dictionary<PlayerId, IEnumerable<JumperId>>(picks)
            : new Dictionary<PlayerId, IEnumerable<JumperId>>());
    }
}
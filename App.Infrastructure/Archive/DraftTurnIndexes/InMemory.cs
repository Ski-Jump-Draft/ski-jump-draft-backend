using System.Collections.Concurrent;
using App.Application.Game.DraftTurnIndexes;

namespace App.Infrastructure.Archive.DraftTurnIndexes;

public class InMemoryDraftTurnIndexesArchive : IDraftTurnIndexesArchive
{
    // gameId -> list of turn indexes
    private readonly ConcurrentDictionary<Guid, List<DraftTurnIndexesDto>> _store = new();

    public Task<List<DraftTurnIndexesDto>> GetAsync(Guid gameId)
    {
        var result = _store.TryGetValue(gameId, out var list)
            ? list
            : [];
        return Task.FromResult(result);
    }

    public Task SetFixedAsync(Guid gameId, List<DraftFixedTurnIndexDto> fixedTurnIndexesDtos)
    {
        var list = fixedTurnIndexesDtos
            .Select(f => new DraftTurnIndexesDto(f.gamePlayerId, f.FixedTurnIndex, new List<int>()))
            .ToList();

        _store[gameId] = list;
        return Task.CompletedTask;
    }

    public Task AddRandomAsync(Guid gameId, Guid gamePlayerId, int turnIndex, int whichPick)
    {
        var list = _store.GetOrAdd(gameId, _ => []);

        var dto = list.FirstOrDefault(x => x.gamePlayerId == gamePlayerId);
        if (dto is null)
        {
            dto = new DraftTurnIndexesDto(gamePlayerId, null, new List<int> { turnIndex });
            list.Add(dto);
        }
        else
        {
            var newIndexes = (dto.TurnIndexes ?? new List<int>()).ToList();
            newIndexes.Add(turnIndex);
            var updated = dto with { TurnIndexes = newIndexes };
            list[list.IndexOf(dto)] = updated;
        }

        return Task.CompletedTask;
    }
}
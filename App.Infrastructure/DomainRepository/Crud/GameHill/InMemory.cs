using System.Collections.Concurrent;
using App.Domain.Game;
using App.Domain.Repositories;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud.GameHill;

public class InMemory : IGameHillRepository
{
    private readonly ConcurrentDictionary<Hill.Id, Hill.Hill> _store
        = new();

    public FSharpAsync<FSharpOption<Hill.Hill>> GetByIdAsync(Hill.Id id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var snapshotBlob);
        FSharpOption<Hill.Hill> opt = snapshotBlob is not null
            ? (snapshotBlob)
            : null!;
        return FSharpAsync.AwaitTask(Task.FromResult(opt));
    }

    public FSharpAsync<Unit> SaveAsync(Hill.Hill hill)
    {
        _store[hill.Id] = hill;
        return FSharpAsync.AwaitTask(Task.CompletedTask);
    }
}
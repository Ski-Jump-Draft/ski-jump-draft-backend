using System.Collections.Concurrent;
using App.Domain.Competition;
using App.Domain.Repositories;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud.CompetitionEngineSnapshot;

public class InMemory : ICompetitionEngineSnapshotRepository
{
    private readonly ConcurrentDictionary<Engine.Id, Engine.EngineSnapshotBlob> _store
        = new();

    public FSharpAsync<FSharpOption<Engine.EngineSnapshotBlob>> GetByIdAsync(Engine.Id id)
    {
        _store.TryGetValue(id, out var snapshotBlob);
        FSharpOption<Engine.EngineSnapshotBlob> opt = snapshotBlob is not null
            ? (snapshotBlob)
            : null!;
        return FSharpAsync.AwaitTask(Task.FromResult(opt));
    }

    public FSharpAsync<Unit> SaveByIdAsync(Engine.Id engine, Engine.EngineSnapshotBlob snapshot)
    {
        _store[engine] = snapshot;
        return FSharpAsync.AwaitTask(Task.CompletedTask);
    }
}
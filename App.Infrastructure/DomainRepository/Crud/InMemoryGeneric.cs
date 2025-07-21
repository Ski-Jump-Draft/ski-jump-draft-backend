using System.Collections.Concurrent;
using App.Domain.Repositories;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud;

public class InMemoryCrudDomainRepository<TId, T> : IDomainCrudRepository<TId, T> where TId : notnull
{
    private readonly ConcurrentDictionary<TId, T> _store
        = new();

    public Task<FSharpOption<T>> GetByIdAsync(TId id)
    {
        _store.TryGetValue(id, out var snapshotBlob);
        FSharpOption<T> opt = snapshotBlob is not null
            ? (snapshotBlob)
            : null!;
        return Task.FromResult(opt);
    }

    public Task SaveAsync(TId id, T value)
    {
        _store[id] = value;
        return Task.CompletedTask;
    }
}
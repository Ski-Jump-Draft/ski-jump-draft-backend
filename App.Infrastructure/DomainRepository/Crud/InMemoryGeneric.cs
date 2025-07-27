using System.Collections.Concurrent;
using App.Domain.Repositories;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud;

public record InMemoryCrudDomainRepositoryStarter<TId, T>(IReadOnlyCollection<T> StarterItems, Func<T, TId> MapToId)
    where TId : notnull;

public class InMemoryCrudDomainRepository<TId, T>(InMemoryCrudDomainRepositoryStarter<TId, T>? starter)
    : IDomainCrudRepository<TId, T> where TId : notnull
{
    private readonly ConcurrentDictionary<TId, T> _store = InitStore(starter);

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

    private static ConcurrentDictionary<TId, T> InitStore(InMemoryCrudDomainRepositoryStarter<TId, T>? starter)
        => starter?.StarterItems.ToDictionary(starter.MapToId)
            is { } dict
            ? new ConcurrentDictionary<TId, T>(dict)
            : new ConcurrentDictionary<TId, T>();
}
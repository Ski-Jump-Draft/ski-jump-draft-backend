using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace App.Infrastructure.DomainRepository.Crud;

public record InMemoryCrudDomainRepositoryStarter<TId, T>(IReadOnlyCollection<T> StarterItems, Func<T, TId> MapToId)
    where TId : notnull;

public class InMemoryCrudDomainRepository<TId, T>(
    IEnumerable<T>? starterItems = null,
    Func<T, TId>? mapToId = null
) : IDomainCrudRepository<TId, T>
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, T> _store = InitStore(starterItems, mapToId);

    public Task<FSharpOption<T>> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var snapshotBlob);
        FSharpOption<T> opt = snapshotBlob is not null
            ? (snapshotBlob)
            : null!;
        return Task.FromResult(opt);
    }

    public Task SaveAsync(TId id, T value, CancellationToken ct = default)
    {
        _store[id] = value;
        return Task.CompletedTask;
    }

    private static ConcurrentDictionary<TId, T> InitStore(IEnumerable<T>? items, Func<T, TId>? mapToId)
    {
        if (items is null || mapToId is null)
            return new ConcurrentDictionary<TId, T>();

        var dict = items.ToDictionary(mapToId);
        return new ConcurrentDictionary<TId, T>(dict);
    }
}

public record InMemoryCrudDomainEventsRepositoryStarter<TId, T>(
    IReadOnlyCollection<T> StarterItems,
    Func<T, TId> MapToId)
    where TId : notnull;

public class InMemoryCrudDomainEventsRepository<T, TId, TPayload>(
    IEventBus eventBus,
    IGuid guid,
    Func<TPayload, int> schemaVersion,
    IClock clock,
    IEnumerable<T>? starterItems = null,
    Func<T, TId>? mapToId = null
) : IDomainCrudEventsRepository<T, TId, TPayload> where TId : notnull
{
    private readonly ConcurrentDictionary<TId, T> _store = InitStore(starterItems, mapToId);

    public Task<FSharpOption<T>> GetByIdAsync(TId id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var snapshotBlob);
        FSharpOption<T> opt = snapshotBlob is not null
            ? (snapshotBlob)
            : null!;
        return Task.FromResult(opt);
    }

    public async Task SaveAsync(TId id, T value, FSharpList<TPayload> payloads, Guid correlationId, Guid causationId,
        CancellationToken ct)
    {
        _store[id] = value;
        var domainEvents = payloads.Select(payload =>
                new DomainEvent<TPayload>(
                    new EventHeader
                    {
                        EventId = guid.NewGuid(),
                        AggregateVersion = 0u,
                        SchemaVer = Convert.ToUInt16(schemaVersion(payload)),
                        OccurredAt = clock.Now,
                        CorrelationId = correlationId,
                        CausationId = causationId
                    },
                    payload))
            .ToList();

        await eventBus.PublishAsync(domainEvents, ct).ConfigureAwait(false);
    }

    private static ConcurrentDictionary<TId, T> InitStore(IEnumerable<T>? items, Func<T, TId>? mapToId)
    {
        if (items is null || mapToId is null)
            return new ConcurrentDictionary<TId, T>();

        var dict = items.ToDictionary(mapToId);
        return new ConcurrentDictionary<TId, T>(dict);
    }
}
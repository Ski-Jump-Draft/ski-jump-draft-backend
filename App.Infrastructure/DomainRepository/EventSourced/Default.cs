using App.Application.Abstractions;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using static Microsoft.FSharp.Collections.ListModule;

namespace App.Infrastructure.DomainRepository.EventSourced;

public abstract class DefaultEventSourcedRepository<TAggregate, TId, TPayload>(
    IEventStore<TId, TPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus,
    Func<IEnumerable<DomainEvent<TPayload>>, TAggregate> evolve,
    Func<TPayload, int> schemaVersion,
    Func<TAggregate, TId> id)
    :
        IEventSourcedRepository<TAggregate, TId, TPayload>
{
    public async Task<FSharpOption<TAggregate>> LoadAsync(TId aggregateId, CancellationToken ct = default)
    {
        var events = await store.LoadAsync(aggregateId, ct).ConfigureAwait(false);
        return events.Count == 0
            ? FSharpOption<TAggregate>.None
            : FSharpOption<TAggregate>.Some(evolve(events));
    }

    public async Task SaveAsync(
        TAggregate aggregate,
        FSharpList<TPayload> payloads,
        AggregateVersion.AggregateVersion expectedVersion,
        Guid correlationId,
        Guid causationId,
        CancellationToken ct = default)
    {
        var domainEvents = payloads.Select(p =>
                new DomainEvent<TPayload>(
                    new EventHeader
                    {
                        EventId = guid.NewGuid(),
                        SchemaVer = Convert.ToUInt16(schemaVersion(p)),
                        OccurredAt = clock.Now,
                        CorrelationId = correlationId,
                        CausationId = causationId
                    },
                    p))
            .ToList();

        await store.AppendAsync(id(aggregate), domainEvents, (int)expectedVersion.Item, ct)
            .ConfigureAwait(false);
        await eventBus.PublishAsync(domainEvents, ct).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(TId aggregateId, CancellationToken ct = default) =>
        (await store.LoadAsync(aggregateId, ct).ConfigureAwait(false)).Count > 0;

    public async Task<AggregateVersion.AggregateVersion> GetVersionAsync(TId aggregateId,
        CancellationToken ct = default) =>
        AggregateVersion.AggregateVersion.NewAggregateVersion(
            (uint)(await store.LoadAsync(aggregateId, ct).ConfigureAwait(false)).Count);

    public async Task<FSharpList<TPayload>> LoadHistoryAsync(TId aggregateId, CancellationToken ct = default) =>
        OfSeq((await store.LoadAsync(aggregateId, ct).ConfigureAwait(false)).Select(e => e.Payload));
}
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
    Func<IEnumerable<DomainEvent<TPayload>>, Task<TAggregate>> evolveAsync,
    Func<TPayload, int> schemaVersion)
    :
        IEventSourcedRepository<TAggregate, TId, TPayload>
{
    public async Task<FSharpOption<TAggregate>> LoadAsync(TId aggregateId, CancellationToken ct = default)
    {
        var events = await store.LoadAsync(aggregateId, ct).ConfigureAwait(false);
        if (events.Count == 0) return FSharpOption<TAggregate>.None;

        var agg = await evolveAsync(events).ConfigureAwait(false);
        return FSharpOption<TAggregate>.Some(agg);
    }


    public async Task SaveAsync(
        TId aggregateId,
        FSharpList<TPayload> payloads,
        AggregateVersion.AggregateVersion expectedVersionAfterSave,
        Guid correlationId,
        Guid causationId,
        CancellationToken ct = default)
    {
        var version = (int)expectedVersionAfterSave.Item;
        var domainEvents = payloads.Select(payload =>
                new DomainEvent<TPayload>(
                    new EventHeader
                    {
                        EventId = guid.NewGuid(),
                        AggregateVersion = (uint)version,
                        SchemaVer = Convert.ToUInt16(schemaVersion(payload)),
                        OccurredAt = clock.Now,
                        CorrelationId = correlationId,
                        CausationId = causationId
                    },
                    payload))
            .ToList();

        await store.AppendAsync(aggregateId, domainEvents, (int)expectedVersionAfterSave.Item, ct)
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
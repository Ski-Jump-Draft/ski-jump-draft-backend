using App.Application.Abstractions;
using App.Domain.Draft;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using static Microsoft.FSharp.Collections.ListModule;

namespace App.Infrastructure.DomainRepository.EventSourced;

public class DraftRepository(
    IEventStore<App.Domain.Draft.Id.Id, Event.DraftEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    : IDraftRepository
{
    public FSharpAsync<FSharpOption<App.Domain.Draft.Draft>> LoadAsync(Id.Id id, CancellationToken ct)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<FSharpOption<App.Domain.Draft.Draft>> Impl()
        {
            var events = await store.LoadAsync(id, ct).ConfigureAwait(false);

            if (events.Count == 0)
                return FSharpOption<App.Domain.Draft.Draft>.None;

            var state = Evolve.evolveFromEvents(OfSeq(events));
            return FSharpOption<App.Domain.Draft.Draft>.Some(state);
        }
    }

    FSharpAsync<Unit>
        IEventSourcedRepository<
            Draft,
            Id.Id,
            Event.DraftEventPayload>.SaveAsync(
            Draft aggregate,
            FSharpList<Event.DraftEventPayload> payloads,
            AggregateVersion.AggregateVersion expectedVersion,
            System.Guid correlationId,
            System.Guid causationId,
            CancellationToken ct)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task Impl()
        {
            var domainEvents =
                payloads.Select(p => new DomainEvent<Event.DraftEventPayload>(
                    new EventHeader
                    {
                        EventId = guid.NewGuid(),
                        SchemaVer = Event.Versioning.schemaVersion(p),
                        OccurredAt = clock.Now,
                        CorrelationId = correlationId,
                        CausationId = causationId
                    },
                    p)).ToList();

            await store.AppendAsync(
                    aggregate.Id_,
                    domainEvents,
                    (int)expectedVersion.Item, // uint32 -> int
                    ct)
                .ConfigureAwait(false);
            await eventBus.PublishAsync(domainEvents, ct);
        }
    }

    public FSharpAsync<bool> ExistsAsync(Id.Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<bool> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            return events.Count > 0;
        }
    }

    public FSharpAsync<AggregateVersion.AggregateVersion> GetVersionAsync(Id.Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<AggregateVersion.AggregateVersion> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            return AggregateVersion.AggregateVersion.NewAggregateVersion((uint)events.Count);
        }
    }

    public FSharpAsync<FSharpList<Event.DraftEventPayload>> LoadHistoryAsync(Id.Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<FSharpList<Event.DraftEventPayload>> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            var payloads = events.Select(e => e.Payload);
            return ListModule.OfSeq(payloads);
        }
    }
}
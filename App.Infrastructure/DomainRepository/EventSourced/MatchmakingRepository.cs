using App.Application.Abstractions;
using App.Domain.Matchmaking;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using static Microsoft.FSharp.Collections.ListModule;
using Event = App.Domain.Matchmaking.Event;
using Id = App.Domain.Matchmaking.Id;

namespace App.Infrastructure.DomainRepository.EventSourced;

public class MatchmakingRepository(
    IEventStore<Id, Event.MatchmakingEventPayload> store,
    IClock clock,
    IGuid guid,
    IEventBus eventBus)
    : IMatchmakingRepository
{
    public FSharpAsync<FSharpOption<Matchmaking>> LoadAsync(Id id, CancellationToken ct)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<FSharpOption<Matchmaking>> Impl()
        {
            var events = await store.LoadAsync(id, ct).ConfigureAwait(false);

            if (events.Count == 0)
                return FSharpOption<Matchmaking>.None;

            var state = Evolve.evolveFromEvents(OfSeq(events));
            return FSharpOption<Matchmaking>.Some(state);
        }
    }

    FSharpAsync<Unit>
        IEventSourcedRepository<
            Matchmaking,
            Id,
            Event.MatchmakingEventPayload>.SaveAsync(
            Matchmaking aggregate,
            FSharpList<Event.MatchmakingEventPayload> payloads,
            AggregateVersion.AggregateVersion expectedVersion,
            System.Guid correlationId,
            System.Guid causationId,
            CancellationToken ct)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task Impl()
        {
            var domainEvents =
                payloads.Select(p => new DomainEvent<Event.MatchmakingEventPayload>(
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

    public FSharpAsync<bool> ExistsAsync(Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<bool> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            return events.Count > 0;
        }
    }

    public FSharpAsync<AggregateVersion.AggregateVersion> GetVersionAsync(Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<AggregateVersion.AggregateVersion> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            return AggregateVersion.AggregateVersion.NewAggregateVersion((uint)events.Count);
        }
    }

    public FSharpAsync<FSharpList<Event.MatchmakingEventPayload>> LoadHistoryAsync(Id id)
    {
        return FSharpAsync.AwaitTask(Impl());

        async Task<FSharpList<Event.MatchmakingEventPayload>> Impl()
        {
            var events = await store.LoadAsync(id, CancellationToken.None)
                .ConfigureAwait(false);
            var payloads = events.Select(e => e.Payload);
            return ListModule.OfSeq(payloads);
        }
    }
}
using App.Application.CSharp.Draft;
using App.Domain.Shared.EventHelpers;

namespace App.Application.CSharp.Shared;

using App.Domain.Draft;

public class DraftRepository : IEventSourcedRepository<Draft, Guid>
{
    private readonly IEventStore _store;
    private readonly IEventBus _bus;

    public DraftRepository(IEventStore store, IEventBus bus) {
        _store = store; _bus = bus;
    }

    public async Task<Draft> LoadAsync(Guid id) {
        var eventsStream = await _store.LoadAsync(id);
        
        var typedStream = eventsStream
            .Select(@event => new DomainEvent<Event.DraftEventPayload>(
                @event.Header,
                (Event.DraftEventPayload)@event.Payload
            ));
        
        return DraftRehydrator.Rehydrate(typedStream);
    }

    public async Task SaveAsync(Guid id, IEnumerable<DomainEvent<object>> newEvents) {
        await _store.AppendAsync(id, newEvents);
        foreach (var ev in newEvents)
            await _bus.PublishAsync(ev);
    }
}

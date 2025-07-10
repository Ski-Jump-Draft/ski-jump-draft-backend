using App.Application.Abstractions;

namespace App.Infrastructure.Repository.Draft;

using App.Domain.Shared;
using App.Domain.Draft;

public class DraftRepository : IEventSourcedRepository<Draft, App.Domain.Draft.Id.Id>
{
    private readonly IEventStore _store;
    private readonly IEventBus _bus;

    public DraftRepository(IEventStore store, IEventBus bus)
    {
        _store = store;
        _bus = bus;
    }

    public async Task<Guid?> GetLastEventIdAsync(App.Domain.Draft.Id.Id draftId)
    {
        var guid = Id.IdModule.value(draftId);
        var events = await _store.LoadAsync(guid);
        if (!events.Any())
            return null;
        var last = events.Last();
        return last.Header.EventId;
    }

    public async Task<Draft> LoadAsync(App.Domain.Draft.Id.Id draftId)
    {
        var guid = Id.IdModule.value(draftId);
        var eventsStream = await _store.LoadAsync(guid);
        var typedStream = eventsStream.Select(e =>
            new DomainEvent<Event.DraftEventPayload>(e.Header, (Event.DraftEventPayload)e.Payload));
        return Rehydrator.rehydrate(draftId, typedStream);
    }

    public async Task SaveAsync(App.Domain.Draft.Id.Id draftId, IReadOnlyList<DomainEvent<object>> newEvents)
    {
        var domainEvents = newEvents.ToList();
        await _store.AppendAsync(Id.IdModule.value(draftId), domainEvents);
        foreach (var ev in domainEvents)
            await _bus.PublishAsync(ev);
    }
}
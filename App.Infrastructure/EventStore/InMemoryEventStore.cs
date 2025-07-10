using App.Application.Abstractions;
using App.Domain.Shared;

namespace App.Infrastructure.EventStore;

class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<Guid, List<DomainEvent<object>>> _storage
        = new Dictionary<Guid, List<DomainEvent<object>>>();

    public Task<IReadOnlyList<DomainEvent<object>>> LoadAsync(Guid id)
    {
        if (!_storage.TryGetValue(id, out var evs))
            return Task.FromResult((IReadOnlyList<DomainEvent<object>>)Array.Empty<DomainEvent<object>>());
        return Task.FromResult((IReadOnlyList<DomainEvent<object>>)evs.ToList());
    }

    public Task AppendAsync(Guid id, IReadOnlyList<DomainEvent<object>> newEvents)
    {
        if (!_storage.TryGetValue(id, out var evs))
            _storage[id] = evs = new List<DomainEvent<object>>();
        evs.AddRange(newEvents);
        return Task.CompletedTask;
    }

    public IReadOnlyList<DomainEvent<object>> AllFor(Guid id)
        => _storage.TryGetValue(id, out var evs) ? evs : Array.Empty<DomainEvent<object>>();
}
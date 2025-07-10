using App.Domain.Shared;

namespace App.Application.Abstractions;

public interface IEventStore
{
    Task<IReadOnlyList<DomainEvent<object>>> LoadAsync(Guid aggregateId);
    Task AppendAsync(Guid aggregateId, IReadOnlyList<DomainEvent<object>> events);
}

public interface IEventBus
{
    Task PublishAsync<T>(DomainEvent<T> @event);
}

public interface IEventHandler<T>
{
    Task HandleAsync(DomainEvent<T> @event);
}

public interface IEventSourcedRepository<TAggregate, TId>
{
    Task<Guid?> GetLastEventIdAsync(TId id);
    Task<TAggregate> LoadAsync(TId id);
    Task SaveAsync(TId id, IReadOnlyList<DomainEvent<object>> newEvents);
}
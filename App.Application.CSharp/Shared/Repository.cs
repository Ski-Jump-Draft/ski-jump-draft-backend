using App.Domain.Shared.EventHelpers;

namespace App.Application.CSharp.Shared;

public interface IEventStore {
  Task<IReadOnlyList<DomainEvent<object>>> LoadAsync(Guid aggregateId);
  Task AppendAsync(Guid aggregateId, IEnumerable<DomainEvent<object>> events);
}

public interface IEventBus {
  Task PublishAsync<T>(DomainEvent<T> @event);
}

public interface IEventSourcedRepository<TAggregate, TItemId> {
  Task<TAggregate> LoadAsync(TItemId id);
  Task SaveAsync(TItemId id, IEnumerable<DomainEvent<object>> newEvents);
}
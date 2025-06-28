using App.Application.Abstractions;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;

namespace App.Infrastructure.EventBus;

class InMemoryEventBus : IEventBus
{
    public readonly List<object> Published = [];

    public Task PublishAsync<T>(DomainEvent<T> ev)
    {
        Published.Add(ev);
        return Task.CompletedTask;
    }
}
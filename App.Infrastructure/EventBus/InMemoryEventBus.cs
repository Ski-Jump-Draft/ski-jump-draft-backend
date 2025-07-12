using App.Application.Abstractions;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;

namespace App.Infrastructure.EventBus;

public class InMemoryEventBus : IEventBus
{
    private readonly List<object> _handlers = new();

    public void RegisterHandler<TPayload>(IEventHandler<TPayload> handler)
    {
        _handlers.Add(handler);
    }

    public async Task PublishAsync<TPayload>(
        IReadOnlyList<DomainEvent<TPayload>> events,
        CancellationToken ct)
    {
        foreach (var domainEvent in events)
        {
            foreach (var handler in _handlers.OfType<IEventHandler<TPayload>>())
            {
                await handler.HandleAsync(domainEvent, ct);
            }
        }
    }
}
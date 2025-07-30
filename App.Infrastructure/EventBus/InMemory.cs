using App.Application.Abstractions;
using App.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.EventBus;

public class InMemory(ILogger<InMemory> logger) : IEventBus
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
        logger.LogDebug("Publishing events: {events} (type = {TPayload})", events.Select(ev => ev.Payload!.GetType()),
            typeof(TPayload));
        foreach (var domainEvent in events)
        {
            foreach (var handler in _handlers.OfType<IEventHandler<TPayload>>())
            {
                logger.LogInformation("Handling event {EventId}", domainEvent.Header.EventId);
                await handler.HandleAsync(domainEvent, ct);
            }
        }
    }
}
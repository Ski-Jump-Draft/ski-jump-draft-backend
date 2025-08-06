using App.Domain.Shared;

namespace App.Application.Commanding;

public interface IEventBus
{
    Task PublishAsync<TPayload>(IReadOnlyList<DomainEvent<TPayload>> events, CancellationToken ct);
}
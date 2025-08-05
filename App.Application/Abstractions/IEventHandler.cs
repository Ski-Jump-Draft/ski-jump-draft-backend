using App.Domain.Shared;

namespace App.Application.Abstractions;

public interface IEventHandler<TPayload>
{
    Task HandleAsync(DomainEvent<TPayload> @event, CancellationToken ct);
}
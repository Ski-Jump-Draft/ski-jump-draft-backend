using App.Domain.Shared;

namespace App.Application.Commanding;

public interface IEventHandler<TPayload>
{
    Task HandleAsync(DomainEvent<TPayload> @event, CancellationToken ct);
}
using App.Domain.Shared;

namespace App.Application.Abstractions;

public interface IProjectionHandler<TPayload>
{
    Task HandleAsync(DomainEvent<TPayload> @event, CancellationToken ct);
}
using App.Domain.Shared;

namespace App.Application.Abstractions;

public interface IEventStore<in TId, TPayload>
{
    Task AppendAsync(
        TId streamId,
        IReadOnlyList<DomainEvent<TPayload>> events,
        int expectedVersion,
        CancellationToken ct
    );

    Task<IReadOnlyList<DomainEvent<TPayload>>> LoadAsync(TId streamId, CancellationToken ct);
}
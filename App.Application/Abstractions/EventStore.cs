using App.Domain.Shared;

namespace App.Application.Commanding;

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
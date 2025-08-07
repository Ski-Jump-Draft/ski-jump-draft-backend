using App.Application.Abstractions;
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;
using Microsoft.FSharp.Collections;

namespace App.Infrastructure.DomainRepository.Crud.GameWorldHill;

using Domain.GameWorld;
using Domain.Repositories;
using Microsoft.FSharp.Core;

public class Predefined(IReadOnlyCollection<Hill> hills, IEventBus eventBus, IGuid guid, IClock clock)
    : IGameWorldHillRepository
{
    private readonly IReadOnlyDictionary<Guid, Domain.GameWorld.Hill> _hillsById =
        hills.ToDictionary(h => h.Id_.Item);

    public Task<FSharpOption<Domain.GameWorld.Hill>> GetByIdAsync(Domain.GameWorld.HillTypes.Id id,
        CancellationToken ct = default)
    {
        var found = _hillsById.TryGetValue(id.Item, out var hill)
            ? FSharpOption<Domain.GameWorld.Hill>.Some(hill)
            : FSharpOption<Domain.GameWorld.Hill>.None;
        return Task.FromResult(found);
    }

    public async Task SaveAsync(HillTypes.Id id, Hill value, FSharpList<Event.HillEventPayload> events,
        Guid correlationId,
        Guid causationId, CancellationToken ct)
    {
        var domainEvents = events.Select(payload =>
                new DomainEvent<Event.HillEventPayload>(
                    new EventHeader
                    {
                        EventId = guid.NewGuid(),
                        AggregateVersion = 0u,
                        SchemaVer = Convert.ToUInt16(Event.Versioning.schemaVersion(payload)),
                        OccurredAt = clock.Now,
                        CorrelationId = correlationId,
                        CausationId = causationId
                    },
                    payload))
            .ToList();
        await eventBus.PublishAsync(domainEvents, ct);
    }
}
using App.Domain.Shared;
using App.Domain.Shared.EventHelpers;
using App.Domain.Time;

namespace App.Application.CSharp.Shared.Events;

public static class DomainEventFactory
{
    public static DomainEvent<T> Create<T>(
        ushort schemaVer,
        IClock clock,
        IGuid guid,
        Guid correlationId,
        Guid? causationId,
        T payload
    )
    {
        var header = new EventHeader(guid.NewGuid(), schemaVer, clock.UtcNow, correlationId, causationId);
        return new DomainEvent<T>(header, payload);
    }
}
using App.Domain.Shared.EventHelpers;

namespace App.Util;

public static class EventHeaderExt
{
    public static EventHeader WithNewId(this EventHeader header, Guid? newId = null)
    {
        return new EventHeader
        {
            EventId = newId ?? Guid.NewGuid(),
            SchemaVer = header.SchemaVer,
            OccurredAt = header.OccurredAt,
            CorrelationId = header.CorrelationId,
            CausationId = header.CausationId
        };
    }
}
using App.Domain.Shared;

namespace App.Application.Commanding;

public record MessageContext(Guid CorrelationId, Guid CausationId)
{
    public static MessageContext New(IGuid guid) => new(guid.NewGuid(), guid.NewGuid());

    public static MessageContext Next(Guid correlationId) => new(correlationId, Guid.NewGuid());
    public MessageContext Next() => new(CorrelationId, Guid.NewGuid());
}
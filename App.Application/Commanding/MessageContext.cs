using App.Domain.Shared;

namespace App.Application.Commanding;

// TODO: Użyjmy interfejsu IGuid
public record MessageContext(Guid CorrelationId, Guid CausationId)
{
    public static MessageContext New(Guid guid) => new(guid, guid);

    public static MessageContext Next(Guid correlationId, IGuid guidGenerator) => new(correlationId,  guidGenerator.NewGuid());
    public MessageContext Next() => new(CorrelationId, Guid.NewGuid());
}
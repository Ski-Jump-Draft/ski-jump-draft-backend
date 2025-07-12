using App.Application.Abstractions;
using App.Domain.Draft;
using App.Domain.Shared;

namespace App.Application.Projection;

public class DraftToGameProjection(IDraftToGameMapStore store) : IProjectionHandler<Domain.Game.Event.GameEventPayload>
{
    public Task HandleAsync(DomainEvent<Domain.Game.Event.GameEventPayload> domainEvent, CancellationToken ct)
    {
        if (domainEvent.Payload is Domain.Game.Event.GameEventPayload.DraftPhaseStartedV1
            {
                Item.GameId: not null
            } payload)
        {
            return store.AddMappingAsync(payload.Item.DraftId, payload.Item.GameId, ct);
        }

        return Task.CompletedTask;
    }
}
using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.Util;
using App.Domain.Draft;
using App.Domain.Repositories;
using App.Domain.Shared;

namespace App.Application.Saga;

public class DraftSaga(
    IDraftToGameMapStore draftToGameMap,
    ICommandBus commands)
    : IEventHandler<Event.DraftEventPayload>
{
    public async Task HandleAsync(DomainEvent<Event.DraftEventPayload> @event, CancellationToken ct)
    {
        if (@event.Payload is Event.DraftEventPayload.DraftEndedV1 draftEndedV1Payload)
        {
            var map = await draftToGameMap.TryGetGameIdByDraftIdAsync(draftEndedV1Payload.Item.DraftId, ct);

            if (!map.Found || map.GameId is null)
                throw new IdNotFoundException<Guid>(draftEndedV1Payload.Item.DraftId.Item);

            var command = new UseCase.Game.EndDraftPhase.Command(map.GameId);
            await commands.SendAsync(command, ct);
        }
    }
}
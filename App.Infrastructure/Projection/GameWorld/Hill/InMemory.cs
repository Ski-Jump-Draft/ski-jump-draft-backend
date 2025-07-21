using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using App.Domain.GameWorld;
using App.Domain.Shared;

namespace App.Infrastructure.Projection.GameWorld.Hill;

public class InMemory : IGameWorldHillProjection, IEventHandler<Event.GameWorldEventPayload>
{
    private readonly ConcurrentDictionary<HillId, GameWorldHillDto> _state = new();

    public Task<IEnumerable<GameWorldHillDto>> GetAllAsync()
    {
        var result = _state.Values.AsEnumerable();
        return Task.FromResult(result);
    }

    public Task HandleAsync(DomainEvent<Event.GameWorldEventPayload> @event, CancellationToken ct)
    {
        switch (@event.Payload)
        {
            case Event.GameWorldEventPayload.HillCreatedV1 payload:
                var dto = new GameWorldHillDto(
                    payload.Item.HillId.Item,
                    payload.Item.Location,
                    payload.Item.CountryId,
                    payload.Item.KPoint,
                    payload.Item.HSPoint
                );
                _state[payload.Item.HillId] = dto;
                break;

            case Event.GameWorldEventPayload.HillRemovedV1 payload:
                _state.TryRemove(payload.Item.HillId, out _);
                break;
        }

        return Task.CompletedTask;
    }
}
using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.ReadModel.Projection;
using App.Domain.GameWorld;
using App.Domain.Shared;

namespace App.Infrastructure.Projection.GameWorld.Hill;

// public class InMemory : IGameWorldHillProjection, IEventHandler<Event.HillEventPayload>
// {
//     private readonly ConcurrentDictionary<HillTypes.Id, GameWorldHillDto> _state = new();
//
//     public Task<IEnumerable<GameWorldHillDto>> GetAllAsync()
//     {
//         var result = _state.Values.AsEnumerable();
//         return Task.FromResult(result);
//     }
//
//     public Task HandleAsync(DomainEvent<Event.HillEventPayload> @event, CancellationToken ct)
//     {
//         switch (@event.Payload)
//         {
//             case Event.HillEventPayload.HillCreatedV1 payload:
//                 var dto = new GameWorldHillDto(
//                     payload.Item.HillId.Item,
//                     payload.Item.Location.Item,
//                     payload.Item.CountryId.Item.ToString(),
//                     HillTypes.KPointModule.value(payload.Item.KPoint),
//                     HillTypes.HsPointModule.value(payload.Item.HsPoint)
//                 );
//                 _state[payload.Item.HillId] = dto;
//                 break;
//
//             case Event.HillEventPayload.HillRemovedV1 payload:
//                 _state.TryRemove(payload.Item.HillId, out _);
//                 break;
//         }
//
//         return Task.CompletedTask;
//     }
// }
using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using App.Domain.Game;
using App.Domain.Shared;

namespace App.Infrastructure.Projection.Game.Participants;

public class InMemory : IGameParticipantsProjection, IEventHandler<Event.GameEventPayload>
{
    private readonly ConcurrentDictionary<System.Guid, IEnumerable<GameParticipantDto>> _store = new();

    public Task<IEnumerable<GameParticipantDto>> GetParticipantsByGameIdAsync(Id.Id gameId)
    {
        return Task.FromResult(_store[gameId.Item]);
    }

    public Task HandleAsync(DomainEvent<Event.GameEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;
        if (payload is Event.GameEventPayload.GameCreatedV1 gameCreated)
        {
            var gameGuid = gameCreated.Item.GameId.Item;
            var participants = gameCreated.Item.Participants;

            var participantDtos = participants.Select(participant =>
                new GameParticipantDto(gameGuid, Participant.NickModule.value(participant.Nick)));
            _store.TryAdd(gameGuid, participantDtos);
        }

        if (payload is Event.GameEventPayload.ParticipantLeftV1 participantLeft)
        {
            var gameGuid = participantLeft.Item.GameId.Item;
            var participantGuid = participantLeft.Item.ParticipantId.Item;

            if (_store.TryGetValue(gameGuid, out var existing))
            {
                var updated = existing
                    .Where(gameParticipantDto => gameParticipantDto.Id != participantGuid)
                    .ToList();

                _store[gameGuid] = updated;
            }
        }

        return Task.CompletedTask;
    }
}
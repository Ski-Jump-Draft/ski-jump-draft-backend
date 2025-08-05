using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.ReadModel.Projection;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Matchmaking.MatchmakingParticipants;

public class InMemory : IMatchmakingParticipantsProjection, IEventHandler<Event.MatchmakingEventPayload>
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, MatchmakingParticipantDto>> _store = new();
    private readonly ConcurrentDictionary<Guid, MatchmakingParticipantDto> _participantsIndex = new();

    public Task<MatchmakingParticipantDto?> GetParticipantById(ParticipantModule.Id id)
        => Task.FromResult(_participantsIndex.GetValueOrDefault(id.Item));

    public Task<IEnumerable<MatchmakingParticipantDto>> GetParticipantsByMatchmakingIdAsync(Id matchmakingId)
        => Task.FromResult<IEnumerable<MatchmakingParticipantDto>>(
            _store.GetValueOrDefault(matchmakingId.Item)?.Values ?? Array.Empty<MatchmakingParticipantDto>()
        );

    public Task HandleAsync(DomainEvent<Event.MatchmakingEventPayload> ev, CancellationToken ct)
    {
        switch (ev.Payload)
        {
            case Event.MatchmakingEventPayload.MatchmakingCreatedV1 payload:
                _store[payload.Item.MatchmakingId.Item] = new ConcurrentDictionary<Guid, MatchmakingParticipantDto>();
                break;

            case Event.MatchmakingEventPayload.MatchmakingParticipantJoinedV1 payload:
            {
                var mmId = payload.Item.MatchmakingId.Item;
                var participantId = payload.Item.Participant.Id.Item;

                var participantDto = new MatchmakingParticipantDto(
                    participantId,
                    ParticipantModule.NickModule.value(payload.Item.Participant.Nick)
                );

                var participants =
                    _store.GetOrAdd(mmId, _ => new ConcurrentDictionary<Guid, MatchmakingParticipantDto>());
                participants[participantId] = participantDto;
                _participantsIndex[participantId] = participantDto;
                break;
            }

            case Event.MatchmakingEventPayload.MatchmakingParticipantLeftV1 payload:
            {
                var mmId = payload.Item.MatchmakingId.Item;
                var participantId = payload.Item.ParticipantId.Item;

                if (_store.TryGetValue(mmId, out var participants))
                    participants.TryRemove(participantId, out _);

                _participantsIndex.TryRemove(participantId, out _);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
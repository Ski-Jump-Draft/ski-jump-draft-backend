using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.Projection;
using App.Application.ReadModel.Projection;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Matchmaking.MatchmakingParticipants;

public class InMemory : IMatchmakingParticipantsProjection, IEventHandler<Event.MatchmakingEventPayload>
{
    private readonly ConcurrentDictionary<System.Guid, IEnumerable<MatchmakingParticipantDto>> _store = new();

    public Task<IEnumerable<MatchmakingParticipantDto>> GetParticipantsByMatchmakingIdAsync(Id matchmakingId) =>
        Task.FromResult(_store.GetValueOrDefault(matchmakingId.Item) ?? []);

    public Task HandleAsync(DomainEvent<Event.MatchmakingEventPayload> ev, CancellationToken ct)
    {
        var occurredAt = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.MatchmakingEventPayload.MatchmakingCreatedV1 payload:
                _store[payload.Item.MatchmakingId.Item] = [];
                break;

            case Event.MatchmakingEventPayload.MatchmakingPlayerJoinedV1 payload:
            {
                var id = payload.Item.MatchmakingId.Item;
                var existing = _store.GetValueOrDefault(id) ?? [];
                var newParticipant = new MatchmakingParticipantDto(
                    payload.Item.ParticipantId.Item
                );
                _store[id] = existing.Append(newParticipant);
                break;
            }

            case Event.MatchmakingEventPayload.MatchmakingPlayerLeftV1 payload:
            {
                var id = payload.Item.MatchmakingId.Item;
                var existing = _store.GetValueOrDefault(id) ?? [];
                _store[id] = existing.Where(participantDto => participantDto.Id != payload.Item.ParticipantId.Item);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
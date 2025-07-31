using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.Projection;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Matchmaking.ActiveMatchmakings;

public class InMemory : IActiveMatchmakingsProjection, IEventHandler<Event.MatchmakingEventPayload>
{
    private readonly ConcurrentDictionary<System.Guid, ActiveMatchmakingDto> _store = new();

    public Task<IEnumerable<ActiveMatchmakingDto>> GetActiveMatchmakingsAsync(CancellationToken ct) =>
        Task.FromResult(_store.Values.AsEnumerable());

    public Task<ActiveMatchmakingDto?> GetActiveMatchmakingAsync(System.Guid matchmakingId, CancellationToken ct) =>
        Task.FromResult(_store.GetValueOrDefault(matchmakingId));

    public Task HandleAsync(DomainEvent<Event.MatchmakingEventPayload> ev, CancellationToken ct)
    {
        var occurredAt = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.MatchmakingEventPayload.MatchmakingCreatedV1 payload:
                var minPlayersCount = PlayersCountModule.value(payload.Item.Settings.MaxParticipants);
                var maxPlayersCount = PlayersCountModule.value(payload.Item.Settings.MinParticipants);
                _store[payload.Item.MatchmakingId.Item] = new ActiveMatchmakingDto(
                    payload.Item.MatchmakingId.Item, 0, minPlayersCount, maxPlayersCount);
                break;

            case Event.MatchmakingEventPayload.MatchmakingParticipantJoinedV1 payload:
                var playersCountAfterJoin = _store[payload.Item.MatchmakingId.Item].CurrentPlayersCount + 1;
                _store[payload.Item.MatchmakingId.Item] = _store[payload.Item.MatchmakingId.Item] with
                {
                    CurrentPlayersCount = playersCountAfterJoin
                };
                break;

            case Event.MatchmakingEventPayload.MatchmakingParticipantLeftV1 payload:
                var playersCountAfterLeave = _store[payload.Item.MatchmakingId.Item].CurrentPlayersCount - 1;
                _store[payload.Item.MatchmakingId.Item] = _store[payload.Item.MatchmakingId.Item] with
                {
                    CurrentPlayersCount = playersCountAfterLeave                };
                break;

            case Event.MatchmakingEventPayload.MatchmakingEndedV1 payload:
                _store.TryRemove(payload.Item.MatchmakingId.Item, out _);
                break;

            case Event.MatchmakingEventPayload.MatchmakingFailedV1 payload:
                _store.TryRemove(payload.Item.MatchmakingId.Item, out _);
                break;
        }

        return Task.CompletedTask;
    }
}
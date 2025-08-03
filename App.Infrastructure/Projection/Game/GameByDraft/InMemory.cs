using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.ReadModel.Projection;
using App.Domain.Game;
using App.Domain.Shared;

namespace App.Infrastructure.Projection.Game.GameByDraft;

public class InMemory : IGameByDraftProjection, IEventHandler<Event.GameEventPayload.DraftPhaseStartedV1>
{
    private readonly ConcurrentDictionary<System.Guid, GameByDraftDto> _store = new();

    public Task<GameByDraftDto?> GetGameByDraftIdAsync(System.Guid draftId, CancellationToken ct) => Task.FromResult(
        _store.GetValueOrDefault(
            draftId));

    public Task HandleAsync(DomainEvent<Event.GameEventPayload.DraftPhaseStartedV1> ev, CancellationToken ct)
    {
        var occurred = ev.Header.OccurredAt;

        var gameId = ev.Payload.Item.GameId.Item;
        var draftId = ev.Payload.Item.DraftId.Item;
        _store[draftId] = new GameByDraftDto(gameId, draftId);

        return Task.CompletedTask;
    }
}
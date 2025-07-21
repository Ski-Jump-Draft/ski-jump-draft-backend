using System.Collections.Concurrent;
using App.Application.Abstractions;
using App.Application.Projection;
using App.Domain.Game;
using App.Domain.Shared;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Game.ActiveGames;

public class InMemory : IActiveGamesProjection, IEventHandler<Event.GameEventPayload>
{
    private readonly ConcurrentDictionary<System.Guid, ActiveGameDto> _store = new();

    public Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct) =>
        Task.FromResult(_store.Values.AsEnumerable());

    public Task<ActiveGameDto?> GetActiveGameAsync(System.Guid gameId, CancellationToken ct) =>
        Task.FromResult(_store.GetValueOrDefault(gameId));

    public Task HandleAsync(DomainEvent<Event.GameEventPayload> ev, CancellationToken ct)
    {
        var occurred = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.GameEventPayload.GameCreatedV1 payload:
                _store[payload.Item.GameId.Item] = new ActiveGameDto(
                    payload.Item.GameId.Item,
                    MapPhase(GameModule.Phase.NewBreak(GameModule.PhaseTag
                        .PreDraftTag)),
                    occurred);
                break;

            case Event.GameEventPayload.PreDraftPhaseStartedV1 payload:
                var preDraftId = Domain.PreDraft.Id.Id.NewId(payload.Item.PreDraftId.Item);
                var preDraftPhase =
                    GameModule.Phase.NewPreDraft(preDraftId);
                UpdatePhase(payload.Item.GameId.Item, MapPhase(preDraftPhase));
                break;

            case Event.GameEventPayload.DraftPhaseStartedV1 payload:
                UpdatePhase(payload.Item.GameId.Item, GamePhase.Draft);
                break;

            case Event.GameEventPayload.CompetitionPhaseStartedV1 payload:
                UpdatePhase(payload.Item.GameId.Item, GamePhase.Competition);
                break;

            case Event.GameEventPayload.GameEndedV1 payload:
                _store.TryRemove(payload.Item.GameId.Item, out _);
                break;
        }

        return Task.CompletedTask;
    }

    private void UpdatePhase(System.Guid gameId, GamePhase phase)
    {
        if (_store.TryGetValue(gameId, out var dto))
            _store[gameId] = dto with { Phase = phase };
    }

    private static GamePhase MapPhase(GameModule.Phase ph) => ph switch
    {
        GameModule.Phase.PreDraft _ => GamePhase.PreDraft,
        GameModule.Phase.Draft _ => GamePhase.Draft,
        GameModule.Phase.Competition _ => GamePhase.Competition,
        GameModule.Phase.Ended _ => GamePhase.Ended,
        GameModule.Phase.Break _ => GamePhase.Break,
        _ => GamePhase.Break
    };
}
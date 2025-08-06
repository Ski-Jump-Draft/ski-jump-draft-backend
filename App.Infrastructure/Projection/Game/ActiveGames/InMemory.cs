using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.ReadModel.Projection;
using App.Domain.Game;
using App.Domain.Shared;
using GameModule = App.Domain.Game.GameModule;

namespace App.Infrastructure.Projection.Game.ActiveGames;

public class InMemory : IActiveGamesProjection, IEventHandler<Event.GameEventPayload>
{
    private readonly ConcurrentDictionary<Guid, (ActiveGameDto Game, ActiveGameTimeLimitsDto TimeLimits)>
        _store = new();

    public Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct) =>
        Task.FromResult(_store.Values.Select(x => x.Game).AsEnumerable());

    public Task<ActiveGameDto?> GetByIdAsync(Domain.Game.Id.Id gameId, CancellationToken ct) =>
        Task.FromResult(_store.TryGetValue(gameId.Item, out var entry) ? entry.Game : null);

    public Task<ActiveGameTimeLimitsDto?> GetTimeLimitsByIdAsync(Id.Id gameId, CancellationToken ct) =>
        Task.FromResult(_store.TryGetValue(gameId.Item, out var entry) ? entry.TimeLimits : null);


    public Task HandleAsync(DomainEvent<Event.GameEventPayload> ev, CancellationToken ct)
    {
        var occurred = ev.Header.OccurredAt;

        switch (ev.Payload)
        {
            case Event.GameEventPayload.GameCreatedV1 payload:
                var settings = payload.Item.Settings;

                var preDraftDelay =
                    (settings.StartingPreDraftPolicy as Settings.PhaseTransitionPolicy.StartingPreDraft.AutoAfter)?.Item
                    ?.Value;
                var draftDelay =
                    (settings.StartingDraftPolicy as Settings.PhaseTransitionPolicy.StartingDraft.AutoAfter)?.Item
                    ?.Value;
                var compDelay =
                    (settings.StartingCompetitionPolicy as Settings.PhaseTransitionPolicy.StartingCompetition.AutoAfter)
                    ?.Item?.Value;
                var endingDelay = (settings.EndingGamePolicy as Settings.PhaseTransitionPolicy.EndingGame.AutoAfter)
                    ?.Item?.Value;

                var dto = new ActiveGameDto(
                    payload.Item.GameId.Item,
                    MapPhase(GameModule.Phase.NewBreak(GameModule.PhaseTag.PreDraftTag)),
                    occurred);

                var limits = new ActiveGameTimeLimitsDto(
                    payload.Item.GameId.Item,
                    BreakBeforePreDraft: preDraftDelay,
                    BreakBeforeDraft: draftDelay,
                    BreakBeforeCompetition: compDelay,
                    BreakBeforeEnding: endingDelay
                );

                _store[payload.Item.GameId.Item] = (dto, limits);
                break;

            case Event.GameEventPayload.PreDraftPhaseStartedV1 payload:
                UpdatePhase(payload.Item.GameId.Item,
                    MapPhase(GameModule.Phase.NewPreDraft(Domain.PreDraft.Id.Id.NewId(payload.Item.PreDraftId.Item))));
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

    private void UpdatePhase(Guid gameId, GamePhase phase)
    {
        if (_store.TryGetValue(gameId, out var x))
            _store[gameId] = (x.Game with { Phase = phase }, x.TimeLimits);
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
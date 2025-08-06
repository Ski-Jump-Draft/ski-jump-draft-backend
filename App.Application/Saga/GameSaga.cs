using App.Application.Commanding;
using App.Application.ReadModel.Projection;
using App.Domain.Shared;

namespace App.Application.Saga;

public class GameSaga(
    IActiveGamesProjection activeGamesProjection,
    IPreDraftToGameMapStore preDraftToGame,
    IDraftToGameMapStore draftToGame,
    ICompetitionToGameMapStore competitionToGame,
    IGuid guid,
    ICommandBus commandBus) : IEventHandler<Domain.Matchmaking.Event.MatchmakingEventPayload>,
    IEventHandler<Domain.PreDraft.Event.PreDraftEventPayload>,
    IEventHandler<Domain.Draft.Event.DraftEventPayload>,
    IEventHandler<Domain.SimpleCompetition.Event.CompetitionEventPayload>,
    IEventHandler<Domain.Game.Event.GameEventPayload>
{
    public async Task HandleAsync(DomainEvent<Domain.Matchmaking.Event.MatchmakingEventPayload> @event,
        CancellationToken ct)
    {
        var payload = @event.Payload;

        switch (payload)
        {
            case Domain.Matchmaking.Event.MatchmakingEventPayload.MatchmakingEndedV1 matchmakingEnded:
                var matchmakingId = matchmakingEnded.Item.MatchmakingId;
                var startGame =
                    new UseCase.Handlers.StartGame.Command(
                        matchmakingId);
                var startDraftEnvelope = new CommandEnvelope<UseCase.Handlers.StartGame.Command, Domain.Game.Id.Id>(
                    startGame,
                    MessageContext.Next(@event.Header.CorrelationId, guid));

                await commandBus.SendAsync(startDraftEnvelope,
                    ct); // TODO: Być może chcemy zacząć grę z lekkim opóźnieniem
                break;
        }
    }

    public async Task HandleAsync(DomainEvent<Domain.PreDraft.Event.PreDraftEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;

        switch (payload)
        {
            case Domain.PreDraft.Event.PreDraftEventPayload.PreDraftEndedV1 preDraftEnded:
            {
                var gameId = await GetGameId(preDraftEnded.Item.PreDraftId);
                if (gameId is null) return;

                var timeLimits = await GetTimeLimits(gameId, ct);

                var endPreDraft = new UseCase.Handlers.EndPreDraft.Command(gameId);
                var endPreDraftEnvelope = new CommandEnvelope<UseCase.Handlers.EndPreDraft.Command>(endPreDraft,
                    MessageContext.Next(@event.Header.CorrelationId, guid));

                var startDraft =
                    new UseCase.Handlers.StartDraft.Command(gameId);
                var startDraftEnvelope = new CommandEnvelope<UseCase.Handlers.StartDraft.Command>(startDraft,
                    MessageContext.Next(@event.Header.CorrelationId, guid));

                await commandBus.SendAsync(endPreDraftEnvelope, ct);
                await commandBus.SendAsync(startDraftEnvelope, ct, delay: timeLimits.BreakBeforeDraft);
                break;
            }
            case Domain.PreDraft.Event.PreDraftEventPayload.PreDraftCompetitionEndedV1
                preDraftCompetitionStarted:
                break;
        }

        return;

        async Task<Domain.Game.Id.Id?> GetGameId(Domain.PreDraft.Id.Id preDraftId)
        {
            return (await preDraftToGame.TryGetGameIdAsync(preDraftId, ct)).GameId;
        }
    }

    public async Task HandleAsync(DomainEvent<Domain.Draft.Event.DraftEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;
        if (payload is Domain.Draft.Event.DraftEventPayload.DraftEndedV1 draftEnded)
        {
            var gameId = await GetGameId(draftEnded.Item.DraftId);
            if (gameId is null) return;

            var timeLimits = await GetTimeLimits(gameId, ct);

            var endDraft = new UseCase.Handlers.EndDraft.Command(gameId);
            var endDraftEnvelope = new CommandEnvelope<UseCase.Handlers.EndDraft.Command>(endDraft,
                MessageContext.Next(@event.Header.CorrelationId, guid));

            var startCompetition =
                new UseCase.Handlers.StartCompetition.Command(
                    gameId);

            var startCompetitionEnvelope =
                new CommandEnvelope<UseCase.Handlers.StartCompetition.Command, Domain.SimpleCompetition.CompetitionId>(
                    startCompetition,
                    MessageContext.Next(@event.Header.CorrelationId, guid));

            await commandBus.SendAsync(endDraftEnvelope, ct);
            await commandBus.SendAsync(startCompetitionEnvelope, ct, delay: timeLimits.BreakBeforeCompetition);
        }


        async Task<Domain.Game.Id.Id?> GetGameId(Domain.Draft.Id.Id draftId)
        {
            return (await draftToGame.TryGetGameIdAsync(draftId, ct)).GameId;
        }
    }

    public async Task HandleAsync(
        DomainEvent<Domain.SimpleCompetition.Event.CompetitionEventPayload> @event,
        CancellationToken ct)
    {
        var payload = @event.Payload;
        switch (payload)
        {
            case Domain.SimpleCompetition.Event.CompetitionEventPayload.CompetitionEndedV1 competitionEnded:
                await HandleCompetitionEndedOrCancelled(competitionEnded.Item.CompetitionId, @event, ct);
                break;
            case Domain.SimpleCompetition.Event.CompetitionEventPayload.CompetitionCancelledV1 competitionCancelled:
                await HandleCompetitionEndedOrCancelled(competitionCancelled.Item.CompetitionId, @event, ct);
                break;
        }
    }

    private async Task HandleCompetitionEndedOrCancelled(Domain.SimpleCompetition.CompetitionId competitionId,
        DomainEvent<Domain.SimpleCompetition.Event.CompetitionEventPayload> @event,
        CancellationToken ct)
    {
        var gameId = (await competitionToGame.TryGetGameIdAsync(competitionId, ct)).GameId;
        if (gameId is null) return;

        var timeLimits = await GetTimeLimits(gameId, ct);

        var endCompetition = new UseCase.Handlers.EndCompetition.Command(gameId);
        var endCompetitionEnvelope = new CommandEnvelope<UseCase.Handlers.EndCompetition.Command>(
            endCompetition,
            MessageContext.Next(@event.Header.CorrelationId, guid));

        var endGame = new UseCase.Handlers.EndGame.Command(gameId);
        var endGameEnvelope =
            new CommandEnvelope<UseCase.Handlers.EndGame.Command>(endGame,
                MessageContext.Next(@event.Header.CorrelationId, guid));

        await commandBus.SendAsync(endCompetitionEnvelope, ct);
        await commandBus.SendAsync(endGameEnvelope, ct, delay: timeLimits.BreakBeforeEnding);
    }

    public async Task HandleAsync(DomainEvent<Domain.Game.Event.GameEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;

        switch (payload)
        {
            case Domain.Game.Event.GameEventPayload.GameCreatedV1 gameCreated:
                var gameId = gameCreated.Item.GameId;
                var timeLimits = await GetTimeLimits(gameId, ct);
                var startPreDraft = new UseCase.Handlers.StartPreDraft.Command(gameId);
                var startPreDraftEnvelope =
                    new CommandEnvelope<UseCase.Handlers.StartPreDraft.Command, UseCase.Handlers.StartPreDraft.Result>(
                        startPreDraft,
                        MessageContext.Next(@event.Header.CorrelationId, guid));

                await commandBus.SendAsync(startPreDraftEnvelope, ct, delay: timeLimits.BreakBeforePreDraft);
                break;
        }
    }

    private async Task<ActiveGameTimeLimitsDto> GetTimeLimits(Domain.Game.Id.Id gameId, CancellationToken ct)
    {
        return await activeGamesProjection.GetTimeLimitsByIdAsync(gameId, ct);
    }
}
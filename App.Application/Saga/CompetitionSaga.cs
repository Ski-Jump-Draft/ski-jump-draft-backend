using App.Application.Abstractions;
using App.Application.Commanding;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.SimpleCompetition.Event;
using App.Util;

namespace App.Application.Saga;

public class CompetitionSaga(
    ICompetitionStartlistProjection competitionStartlistProjection,
    ICompetitionToGameMapStore competitionToGame,
    ICommandBus commandBus,
    IGuid guid)
    : IEventHandler<Domain.SimpleCompetition.Event.CompetitionEventPayload>
{
    public async Task HandleAsync(DomainEvent<CompetitionEventPayload> @event, CancellationToken ct)
    {
        var payload = @event.Payload;
        switch (payload)
        {
            case Domain.SimpleCompetition.Event.CompetitionEventPayload.JumpAddedV1 jumpAdded:
                await SendAjustGateCommand(@event, ct, jumpAdded);
                // await SendUpdateWindConditionsCommand(@event, ct, jumpAdded);
                await SendDelayedSimulateJumpCommand(@event, ct, jumpAdded);
                break;
        }
    }

    private async Task SendAjustGateCommand(DomainEvent<CompetitionEventPayload> @event, CancellationToken ct,
        CompetitionEventPayload.JumpAddedV1 jumpAdded)
    {
        var gameId = (await competitionToGame.TryGetGameIdAsync(jumpAdded.Item.CompetitionId, ct)).GameId;
        if (gameId is null) return;

        var command = new UseCase.Handlers.AdjustGate.Command(gameId);
        var envelope = new CommandEnvelope<UseCase.Handlers.AdjustGate.Command>(command,
            MessageContext.Next(@event.Header.CorrelationId, guid));

        await commandBus.SendAsync(envelope, ct);
    }

    private async Task SendDelayedSimulateJumpCommand(DomainEvent<CompetitionEventPayload> @event, CancellationToken ct,
        CompetitionEventPayload.JumpAddedV1 jumpAdded)
    {
        var competitionId = jumpAdded.Item.CompetitionId;
        var nextCompetitorDto =
            await competitionStartlistProjection.GetNextCompetitorByCompetitionIdAsync(competitionId);

        if (nextCompetitorDto is null) return;

        var gameId = (await competitionToGame.TryGetGameIdAsync(competitionId, ct)).GameId;
        if (gameId is null) return;

        var command = new UseCase.Handlers.SimulateJump.Command(gameId);
        var envelope = new CommandEnvelope<UseCase.Handlers.SimulateJump.Command>(command,
            MessageContext.Next(@event.Header.CorrelationId, guid));

        // TODO: Nie używać sztywnych 10 sekund, ale ustalić gdzieś odstep auto-skoków
        await commandBus.SendAsync(envelope, ct, delay: TimeSpan.FromSeconds(10));
    }
}
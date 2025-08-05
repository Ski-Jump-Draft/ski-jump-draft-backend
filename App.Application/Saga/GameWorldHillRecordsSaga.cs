using App.Application.Abstractions;
using App.Application.Exception;
using App.Application.Ext;
using App.Application.ReadModel.Projection;
using App.Domain.Repositories;
using App.Domain.Shared;
using App.Domain.SimpleCompetition.Event;
using App.Domain.Time;
using App.Util;

namespace App.Application.Saga;

public class
    GameWorldHillRecordsSaga(
        ICommandBus commandBus,
        ICompetitionGameWorldHillProjection competitionGameWorldHillProjection,
        ICompetitorProjection competitorProjection)
    : IEventHandler<Domain.SimpleCompetition.Event.CompetitionEventPayload.JumpAddedV1>
{
    public async Task HandleAsync(DomainEvent<CompetitionEventPayload.JumpAddedV1> @event, CancellationToken ct)
    {
        var payload = @event.Payload.Item;
        var jump = payload.Jump;
        var jumpId = jump.Id;
        var competitorId = jump.CompetitorId;
        var competitionId = payload.CompetitionId;
        var jumpDistance = jump.Distance;

        var gameWorldHillDto = await competitionGameWorldHillProjection.GetByCompetitionId(competitionId);
        if (gameWorldHillDto is null)
        {
            throw new InvalidOperationException($"GameWorld.Hill not found for Competition Id: {competitionId}");
        }

        var gameWorldHillId = Domain.GameWorld.HillTypes.Id.NewId(gameWorldHillDto.Id);

        var competitor = await competitorProjection.GetByCompetitionJumpIdAsync(jumpId)
            .AwaitOrWrapNullable(_ => new IdNotFoundException<Guid>(competitorId.Item));

        var gameWorldJumperId = Domain.GameWorld.JumperTypes.Id.NewId(competitor.GameWorldJumperId);

        var potentialRecordSetter = Domain.GameWorld.HillTypes.RecordModule.Setter
            .NewGameWorldJumper(gameWorldJumperId);
        var potentialRecordDistance =
            Domain.GameWorld.HillTypes.RecordModule.DistanceModule.tryCreate(jumpDistance).ResultValue;

        var command = new UseCase.GameWorld.TryUpdateInGameRecords.Command(gameWorldHillId,
            new Domain.GameWorld.HillTypes.Record(potentialRecordSetter, potentialRecordDistance));
        var envelope = new CommandEnvelope<UseCase.GameWorld.TryUpdateInGameRecords.Command>(command,
            MessageContext.Next(@event.Header.CorrelationId));
        await commandBus.SendAsync(envelope, ct);
    }
}
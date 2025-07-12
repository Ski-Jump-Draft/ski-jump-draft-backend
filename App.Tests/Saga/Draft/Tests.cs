using App.Application.Saga;
using App.Domain.Game;
using App.Domain.Shared.Utils;
using App.Infrastructure.CommandBus;
using FluentAssertions;
using Event = App.Domain.Draft.Event;

namespace App.Tests.Saga.Draft;

public class DraftSagaTests
{
    [Fact]
    public async Task Saga_emits_GameEndDraft_when_DraftEnded()
    {
        var draftId = App.Domain.Draft.Id.Id.NewId(Guid.NewGuid());
        var gameId = App.Domain.Game.Id.Id.NewId(Guid.NewGuid());

        var commandBus = new TestCommandBus();
        var mapStore = new App.Infrastructure.DraftToGameMapStore.InMemoryDraftToGameMapStore();
        await mapStore.AddMappingAsync(draftId, gameId, CancellationToken.None);

        var saga = new DraftSaga(mapStore, commandBus);

        var correlationId = Guid.NewGuid();
        var causationId = correlationId;

        var eventPayload = Event.DraftEventPayload.NewDraftEndedV1(new Event.DraftEndedV1(draftId));

        var @event = DomainEventFactory.create(1, DateTimeOffset.UtcNow, Guid.NewGuid(), correlationId,
            causationId,
            eventPayload);

        await saga.HandleAsync(@event, CancellationToken.None);


        commandBus.WasSent<App.Application.UseCase.Game.EndDraftPhase.Command>(command =>
                command.GameId.Item == gameId.Item)
            .Should()
            .BeTrue();
    }
}
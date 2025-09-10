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

        // 1) zamiast InMemoryCommandBus, użyjemy SpyCommandBus
        var spyBus = new SpyCommandBus();

        var mapStore = new App.Infrastructure.DraftToGameMapStore
            .InMemoryDraftToGameMapStore();
        await mapStore.AddMappingAsync(draftId, gameId, CancellationToken.None);

        // 2) podajemy spyBus do sagi
        var saga = new DraftSaga(mapStore, spyBus);

        var correlationId = Guid.NewGuid();
        var causationId = correlationId;

        var payload = Event.DraftEventPayload
            .NewDraftEndedV1(new Event.DraftEndedV1(draftId));

        var @event = DomainEventFactory.create(
            1,
            1,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            correlationId,
            causationId,
            payload
        );

        // 3) wywołujemy sargę
        await saga.HandleAsync(@event, CancellationToken.None);

        // 4) asercja na spyBus
        spyBus
            .WasSent<App.Application.UseCase.Game.EndDraftPhase.Command>(cmd
                => cmd.GameId.Item == gameId.Item)
            .Should()
            .BeTrue();
    }
}
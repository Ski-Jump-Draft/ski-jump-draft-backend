using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using App.Application.Commanding;
using App.Application.Projection;
using App.Domain.Game;
using App.Domain.Shared;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hub.Game;

public class GameNotifierByDomainEvents(
    IHubContext<GameHub> hub,
    IGamePhasePlan gamePhasePlan,
    IActiveGamesProjection activeGames)
    : IEventHandler<Event.GameEventPayload>
{
    public async Task HandleAsync(DomainEvent<Event.GameEventPayload> @event, CancellationToken ct)
    {
        var header = @event.Header;

        switch (@event.Payload)
        {
            case Event.GameEventPayload.GameCreatedV1 gameCreatedEvent:
            {
                var gameId = gameCreatedEvent.Item.GameId.Item;
                var game = await activeGames.GetActiveGameAsync(gameId, ct);
                if(game is null)
                    throw new NullReferenceException($"No active game found for gameId: {gameId} despite GameCreatedV1");
                await Utilities.SendGameStarted(game.GameId, header.OccurredAt, NextPhaseScheduledAt(gameId),
                    header.OccurredAt, hub.Clients.All, ct);
                break;
            }
            case Event.GameEventPayload.DraftPhaseStartedV1 preDraftPhaseStartedPayload:
            {
                var gameId = preDraftPhaseStartedPayload.Item.GameId.Item;
                var game = await activeGames.GetActiveGameAsync(gameId, ct);
                await hub.Clients.Group(gameId.ToString()).SendAsync("preDraftStarted", new
                {
                    // TODO: Wyślij coś do UI na początek draftu
                }, cancellationToken: ct);
                break;
            }
        }

        return;

        DateTimeOffset NextPhaseScheduledAt(Guid gameId) => gamePhasePlan.GetNextPhase(gameId).ScheduledAt;
    }
}
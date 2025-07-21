using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using App.Application.Abstractions;
using App.Application.Projection;
using App.Domain.Matchmaking;
using App.Domain.Shared;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hub.Matchmaking;

public class MatchmakingNotifierByDomainEvents(
    IHubContext<MatchmakingHub> hub,
    IActiveMatchmakingsProjection activeMatchmakings)
    : IEventHandler<Event.MatchmakingEventPayload>
{
    public async Task HandleAsync(DomainEvent<Event.MatchmakingEventPayload> @event, CancellationToken ct)
    {
        switch (@event.Payload)
        {
            case Event.MatchmakingEventPayload.MatchmakingPlayerJoinedV1 playerJoinedEvent:
            {
                var matchmakingId = playerJoinedEvent.Item.MatchmakingId.Item;
                var matchmaking = await activeMatchmakings.GetActiveMatchmakingAsync(matchmakingId, ct);
                ValidateActiveMatchmaking(matchmaking, matchmakingId, "MatchmakingPlayerJoinedV1");
                await hub.Clients.Group(matchmakingId.ToString()).SendAsync("updated", new
                {
                    CurrentPlayersCount = matchmaking!.CurrentPlayersCount,
                    MaxPlayersCount = matchmaking.MaxPlayersCount,
                }, cancellationToken: ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingPlayerLeftV1 playerLeftEvent:
            {
                var matchmakingId = playerLeftEvent.Item.MatchmakingId.Item;
                var matchmaking = await activeMatchmakings.GetActiveMatchmakingAsync(matchmakingId, ct);
                ValidateActiveMatchmaking(matchmaking, matchmakingId, "MatchmakingPlayerLeftV1");
                await hub.Clients.Group(matchmakingId.ToString()).SendAsync("updated", new
                {
                    CurrentPlayersCount = matchmaking!.CurrentPlayersCount,
                    MaxPlayersCount = matchmaking.MaxPlayersCount,
                }, cancellationToken: ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingEndedV1 matchmakingEndedEvent:
            {
                var matchmakingId = matchmakingEndedEvent.Item.MatchmakingId.Item;
                await hub.Clients.Group(matchmakingId.ToString()).SendAsync("ended", new
                {
                    PlayersCount = matchmakingEndedEvent.Item.PlayersCount,
                }, cancellationToken: ct);
                break;
            }
            case Event.MatchmakingEventPayload.MatchmakingFailedV1 matchmakingFailedEvent:
            {
                var matchmakingId = matchmakingFailedEvent.Item.MatchmakingId.Item;
                await hub.Clients.Group(matchmakingId.ToString()).SendAsync("failed", new
                {
                    PlayersCount = matchmakingFailedEvent.Item.PlayersCount,
                    Reason = matchmakingFailedEvent.Item.Error.ToString(),
                }, cancellationToken: ct);
                break;
            }
        }
    }

    private static void ValidateActiveMatchmaking(ActiveMatchmakingDto? matchmaking, Guid matchmakingId,
        string eventName)
    {
        if (matchmaking is null)
        {
            throw new NullReferenceException($"No active matchmaking found for matchmakingId: {matchmakingId} despite {
                eventName}");
        }
    }
}
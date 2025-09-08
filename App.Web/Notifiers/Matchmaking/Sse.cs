using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Web.Notifiers.SseHub;

namespace App.Web.Notifiers.Matchmaking;

public class Sse(ISseHub sse, IJson json) : IMatchmakingNotifier
{
    public Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking)
    {
        var payload = json.Serialize(matchmaking);
        return sse.PublishAsync(matchmaking.MatchmakingId, "matchmaking-updated", payload, CancellationToken.None);
    }

    public Task PlayerJoined(PlayerJoinedDto playerJoined)
    {
        var payload = json.Serialize(playerJoined);
        return sse.PublishAsync(playerJoined.MatchmakingId, "matchmaking-player-joined", payload,
            CancellationToken.None);
    }

    public Task PlayerLeft(PlayerLeftDto playerLeft)
    {
        var payload = json.Serialize(playerLeft);
        return sse.PublishAsync(playerLeft.MatchmakingId, "matchmaking-player-left", payload, CancellationToken.None);
    }
}
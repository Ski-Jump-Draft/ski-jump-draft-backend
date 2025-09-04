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
}

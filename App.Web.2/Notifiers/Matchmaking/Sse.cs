using App.Application._2.Messaging.Notifiers;
using App.Application._2.Utility;
using App.Web._2.Notifiers.SseHub;

namespace App.Web._2.Notifiers.Matchmaking;

public class Sse(ISseHub sse, IJson json) : IMatchmakingNotifier
{
    public Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking)
    {
        var payload = json.Serialize(matchmaking);
        return sse.PublishAsync(matchmaking.MatchmakingId, "matchmaking-updated", payload, CancellationToken.None);
    }
}

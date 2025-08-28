using App.Application._2.Messaging.Notifiers;
using App.Web._2.SignalR.Hub;
using Microsoft.AspNetCore.SignalR;

namespace App.Web._2.Notifiers.Game;

public class SignalRGameNotifier(IHubContext<GameHub> hub) : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId)
    {
        return hub.Clients.Group(GameHub.GroupNameForMatchmaking(matchmakingId))
            .SendAsync("GameStartedAfterMatchmaking", new { MatchmakingId = matchmakingId, GameId = gameId });
    }

    public Task GameUpdated(GameUpdatedDto dto)
    {
        return hub.Clients.Group(GameHub.GroupNameForGame(dto.GameId))
            .SendAsync("GameUpdated", dto);
    }
}

using System.Text.Json;
using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Web.SignalR.Hub;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Notifiers.Game;

public class SignalRGameNotifier(IHubContext<GameHub> hub, IMyLogger logger) : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId,
        Dictionary<Guid, Guid> playersMapping)
    {
        return hub.Clients.Group(GameHub.GroupNameForMatchmaking(matchmakingId))
            .SendAsync("GameStartedAfterMatchmaking",
                new { MatchmakingId = matchmakingId, GameId = gameId, PlayersMapping = playersMapping });
    }

    public async Task GameUpdated(GameUpdatedDto dto)
    {
        // logger.Info("GameUpdated to SignalR", dto);
        logger.Debug("GameUpdated to SignalR", JsonSerializer.Serialize(dto));
        await hub.Clients.Group(GameHub.GroupNameForGame(dto.GameId))
            .SendAsync("GameUpdated", dto);
    }

    public Task GameEnded(Guid gameId)
    {
        logger.Debug($"GameEnded: {gameId}");
        // Już wysłaliśmy GameUpdated mający stan Ended.
        return Task.CompletedTask;
    }
}
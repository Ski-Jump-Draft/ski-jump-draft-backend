using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hub.Game;

public static class Utilities
{
    public static async Task SendGameStarted(Guid gameId, DateTimeOffset eventTimestamp, DateTimeOffset scheduledNextPhaseAt, DateTimeOffset serverTime,  IClientProxy clientProxy,
        CancellationToken ct)
    {
        await clientProxy.SendAsync("gameCreated", new
        {
            gameId = gameId,
            eventTimestamp = eventTimestamp,
            scheduledNextPhaseAt = scheduledNextPhaseAt,
            serverTime = serverTime,
            
        }, cancellationToken: ct);
    }
}
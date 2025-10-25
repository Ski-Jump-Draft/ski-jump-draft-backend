using App.Application.Utility;

namespace App.Web.SignalR.Hub;

// GameHub.cs
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class GameHub(IMyLogger logger, App.Web.Security.IPlayerTokenService tokenService, App.Web.Security.IGamePlayerMappingStore mappingStore) : Hub
{
    // klient wywołuje, by dołączyć do grupy matchmakingowej
    public Task JoinMatchmaking(Guid matchmakingId, Guid playerId, string sig)
    {
        logger.Debug($"JoinMatchmaking (WS): {matchmakingId}");
        if (!tokenService.VerifyMatchmaking(matchmakingId, playerId, sig))
            throw new HubException("Unauthorized");
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));
    }

    public Task LeaveMatchmaking(Guid matchmakingId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));

    // klient wywołuje, by dołączyć do grupy gry
    public Task JoinGame(Guid gameId, Guid playerId, string sig)
    {
        logger.Debug($"JoinGame (WS): {gameId}");
        var ok = tokenService.VerifyGame(gameId, playerId, sig);
        if (!ok)
        {
            if (mappingStore.TryGetByGame(gameId, out var mmId, out var map))
            {
                var mmPlayer = map.FirstOrDefault(kv => kv.Value == playerId).Key;
                if (mmPlayer != Guid.Empty)
                {
                    ok = tokenService.VerifyMatchmaking(mmId, mmPlayer, sig);
                }
            }
        }
        if (!ok) throw new HubException("Unauthorized");
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForGame(gameId));
    }

    public Task LeaveGame(Guid gameId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForGame(gameId));

    // helpery grup
    public static string GroupNameForMatchmaking(Guid id) => $"matchmaking:{id}";
    public static string GroupNameForGame(Guid id) => $"game:{id}";

    // opcjonalnie expose RPC dla klientów, np. request snapshot
    public Task RequestGameSnapshot(Guid gameId)
        => Clients.Caller.SendAsync("RequestGameSnapshotAck", gameId);
}

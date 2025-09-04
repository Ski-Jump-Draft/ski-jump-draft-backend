using App.Application.Utility;

namespace App.Web.SignalR.Hub;

// GameHub.cs
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class GameHub(IMyLogger logger) : Hub
{
    // klient wywołuje, by dołączyć do grupy matchmakingowej
    public Task JoinMatchmaking(Guid matchmakingId)
    {
        logger.Debug($"JoinMatchmaking (WS): {matchmakingId}");
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));
    }

    public Task LeaveMatchmaking(Guid matchmakingId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));

    // klient wywołuje, by dołączyć do grupy gry
    public Task JoinGame(Guid gameId)
    {logger.Debug($"JoinGame (WS): {gameId}");
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

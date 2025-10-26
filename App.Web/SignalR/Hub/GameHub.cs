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
        var tp = string.IsNullOrWhiteSpace(sig) ? "<none>" : (sig.Length <= 12 ? sig : sig[..12] + "…");
        logger.Info($"WS JoinMatchmaking: mmId={matchmakingId}, playerId={playerId}, tokenPrefix={tp}");
        if (!tokenService.VerifyMatchmaking(matchmakingId, playerId, sig))
        {
            logger.Warn($"WS JoinMatchmaking unauthorized: mmId={matchmakingId}, playerId={playerId}, prefix={tp}");
            throw new HubException("Unauthorized");
        }
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));
    }

    public Task LeaveMatchmaking(Guid matchmakingId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));

    // klient wywołuje, by dołączyć do grupy gry
    public Task JoinGame(Guid gameId, Guid playerId, string sig)
    {
        var tp = string.IsNullOrWhiteSpace(sig) ? "<none>" : (sig.Length <= 12 ? sig : sig[..12] + "…");
        logger.Info($"WS JoinGame: gameId={gameId}, playerId={playerId}, tokenPrefix={tp}");
        var ok = tokenService.VerifyGame(gameId, playerId, sig);
        if (!ok)
        {
            if (mappingStore.TryGetByGame(gameId, out var mmId, out var map))
            {
                var mmPlayer = map.FirstOrDefault(kv => kv.Value == playerId).Key;
                if (mmPlayer != Guid.Empty)
                {
                    ok = tokenService.VerifyMatchmaking(mmId, mmPlayer, sig);
                    if (ok)
                    {
                        logger.Info($"WS JoinGame: fallback via matchmaking token OK (gameId={gameId}, mmId={mmId}, mmPlayer={mmPlayer})");
                    }
                }
            }
        }
        if (!ok)
        {
            logger.Warn($"WS JoinGame unauthorized: gameId={gameId}, playerId={playerId}, prefix={tp}");
            throw new HubException("Unauthorized");
        }
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

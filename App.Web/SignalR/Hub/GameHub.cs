using App.Application.Utility;
using App.Application.Commanding;
using App.Application.Extensions;

namespace App.Web.SignalR.Hub;

// GameHub.cs
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class GameHub(IMyLogger logger,
    App.Web.Security.IPlayerTokenService tokenService,
    App.Web.Security.IGamePlayerMappingStore mappingStore,
    ISignalRConnectionTracker wsTracker,
    App.Web.Sse.ISseConnectionTracker sseTracker,
    ICommandBus commandBus) : Hub
{
    // klient wywołuje, by dołączyć do grupy matchmakingowej
    public async Task JoinMatchmaking(Guid matchmakingId, Guid playerId, string sig)
    {
        var tp = string.IsNullOrWhiteSpace(sig) ? "<none>" : (sig.Length <= 12 ? sig : sig[..12] + "…");
        logger.Info($"WS JoinMatchmaking: mmId={matchmakingId}, playerId={playerId}, tokenPrefix={tp}");
        if (!tokenService.VerifyMatchmaking(matchmakingId, playerId, sig))
        {
            logger.Warn($"WS JoinMatchmaking unauthorized: mmId={matchmakingId}, playerId={playerId}, prefix={tp}");
            throw new HubException("Unauthorized");
        }
        // track this connection as active for (mmId, playerId)
        var afterInc = wsTracker.Increment(matchmakingId, playerId);
        Context.Items["mmId"] = matchmakingId;
        Context.Items["playerId"] = playerId;
        logger.Info($"WS connections: incremented count for (mmId={matchmakingId}, playerId={playerId}) -> {afterInc}");
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));
    }

    public async Task LeaveMatchmaking(Guid matchmakingId)
    {
        var pid = Context.Items.TryGetValue("playerId", out var p) && p is Guid g ? g : Guid.Empty;
        if (pid != Guid.Empty)
        {
            var left = wsTracker.Decrement(matchmakingId, pid);
            logger.Info($"WS LeaveMatchmaking called: decremented count (mmId={matchmakingId}, playerId={pid}) -> {left}");
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForMatchmaking(matchmakingId));
    }

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

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var mmId = Context.Items.TryGetValue("mmId", out var mm) && mm is Guid gmm ? gmm : Guid.Empty;
            var pid = Context.Items.TryGetValue("playerId", out var p) && p is Guid gpid ? gpid : Guid.Empty;
            if (mmId != Guid.Empty && pid != Guid.Empty)
            {
                var afterDec = wsTracker.Decrement(mmId, pid);
                var corr = Guid.NewGuid();
                logger.Info($"WS disconnected: scheduling auto-leave check in 250ms (mmId={mmId}, playerId={pid}, wsLeftAfterDec={afterDec}, corr={corr})");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(250);
                        var ws = wsTracker.GetCount(mmId, pid);
                        var sse = sseTracker.GetCount(mmId, pid);
                        if (ws <= 0 && sse <= 0)
                        {
                            var cmd = new App.Application.UseCase.Matchmaking.LeaveMatchmaking.Command(mmId, pid);
                            logger.Info($"WS auto-leave confirmed (no active WS/SSE). Issuing leave (mmId={mmId}, playerId={pid}, corr={corr})");
                            commandBus.SendAsync(cmd, CancellationToken.None).FireAndForget(logger);
                        }
                        else
                        {
                            logger.Info($"WS auto-leave skipped: ws={ws}, sse={sse} active (mmId={mmId}, playerId={pid}, corr={corr})");
                        }
                    }
                    catch (Exception ex)
                    {
                        try { logger.Warn($"WS auto-leave delayed check failed: {ex.Message} (mmId={mmId}, playerId={pid})"); } catch { }
                    }
                });
            }
        }
        catch { }
        return base.OnDisconnectedAsync(exception);
    }

    // helpery grup
    public static string GroupNameForMatchmaking(Guid id) => $"matchmaking:{id}";
    public static string GroupNameForGame(Guid id) => $"game:{id}";

    // opcjonalnie expose RPC dla klientów, np. request snapshot
    public Task RequestGameSnapshot(Guid gameId)
        => Clients.Caller.SendAsync("RequestGameSnapshotAck", gameId);
}

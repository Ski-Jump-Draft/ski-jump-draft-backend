using App.Application.Commanding;
using App.Domain.Time;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hub.Game;

public class GameHub(ICommandBus commandBus, IGamePhasePlan gamePhasePlan, IClock clock) : Microsoft.AspNetCore.SignalR.Hub
{
    /// <summary>
    /// Called by the client once they have gameId & participantId.
    /// </summary>
    public async Task JoinGroup(string matchmakingId, string participantId, CancellationToken ct)
    {
        var createQuickGameCommand =
            new Application.UseCase.Game.QuickGame.Create.Command(Guid.Parse(matchmakingId));
        var game = await commandBus
            .SendAsync<Application.UseCase.Game.QuickGame.Create.Command, App.Domain.Game.Game>(
                createQuickGameCommand, ct);
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id_.ToString(), ct);
        
        var utcNow = clock.Now;
        var scheduledNextPhaseAtUtc = gamePhasePlan.GetNextPhase(game.Id_.Item).ScheduledAt;
        await Utilities.SendGameStarted(game.Id_.Item, utcNow, scheduledNextPhaseAtUtc, utcNow,
            Clients.Caller, ct);
    }

    /// <summary>
    /// (Optional) Remove from a group on disconnect or explicit leave.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
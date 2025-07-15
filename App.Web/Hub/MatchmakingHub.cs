using System.Text.RegularExpressions;

namespace App.Web.Hub;

public class MatchmakingHub : Microsoft.AspNetCore.SignalR.Hub
{
    /// <summary>
    /// Called by the client once they have gameId & participantId.
    /// </summary>
    public Task JoinGroup(string gameId, string participantId) => Groups.AddToGroupAsync(Context.ConnectionId, gameId);

    /// <summary>
    /// (Optional) Remove from a group on disconnect or explicit leave.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
using System.Text.RegularExpressions;

namespace App.Web.Hub.Matchmaking;

public class MatchmakingHub : Microsoft.AspNetCore.SignalR.Hub
{
    /// <summary>
    /// Called by the client once they have matchmakingId & participantId.
    /// </summary>
    public Task JoinGroup(string matchmakingId) => Groups.AddToGroupAsync(Context.ConnectionId, matchmakingId);

    /// <summary>
    /// (Optional) Remove from a group on disconnect or explicit leave.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
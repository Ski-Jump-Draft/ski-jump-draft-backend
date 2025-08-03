// using Microsoft.AspNetCore.SignalR;
//
// namespace App.Web.Hub.Game;
//
// public class GameNotifierForWebSpecificPhases(IHubContext<GameHub> hub)
// {
//     public async Task NotifyHillChoiceStarted(Guid gameId, CancellationToken ct)
//     {
//         await hub.Clients.Group(gameId.ToString())
//             .SendAsync("hillChoiceStarted", new { gameId = gameId }, cancellationToken: ct);
//     }
//
//     public async Task NotifyHillChoiceEnded(Guid gameId, CancellationToken ct)
//     {
//         await hub.Clients.Group(gameId.ToString())
//             .SendAsync("hillChoiceEnded", new { gameId = gameId }, cancellationToken: ct);
//     }
// }
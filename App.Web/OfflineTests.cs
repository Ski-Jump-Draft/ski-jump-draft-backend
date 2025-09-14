using System.Diagnostics;
using App.Application.Commanding;
using App.Application.OfflineTests;
using App.Application.Utility;

namespace App.Web;

public static class OfflineTests
{
    public static async Task InitializeOfflineTest(IServiceProvider sp, IMyLogger logger)
    {
        var myPlayer = sp.GetRequiredService<IMyPlayer>();

        var commandBus = sp.GetRequiredService<ICommandBus>();
        var joinResult = await commandBus
            .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(
                new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(myPlayer.GetNick(), IsBot: false),
                CancellationToken.None);

        logger.Info($"Dołączyłeś do matchmakingu (ID = {joinResult.MatchmakingId})");

        myPlayer.SetMatchmakingId(joinResult.MatchmakingId);
        myPlayer.SetMatchmakingPlayerId(joinResult.PlayerId);
    }
}
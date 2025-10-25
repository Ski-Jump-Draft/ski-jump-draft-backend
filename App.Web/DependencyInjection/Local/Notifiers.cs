using System.Diagnostics;
using App.Application.Messaging.Notifiers;
using App.Application.OfflineTests;
using App.Application.Utility;
using App.Web.Notifiers.SseHub;
using App.Web.SignalR.Hub;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.DependencyInjection.Local;

public static class Notifiers
{
    public static IServiceCollection AddLocalNotifiers(this IServiceCollection services)
    {
        services.AddSingleton<IMatchmakingNotifier, Web.Notifiers.Matchmaking.Sse>();
        services.AddSingleton<ISseHub, App.Web.Notifiers.SseHub.Default>();

        services.AddSingleton<IGameNotifier, App.Application.Messaging.Notifiers.ComposeGameNotifier>(sp =>
        {
            var logger = sp.GetRequiredService<IMyLogger>();
            var myPlayer = sp.GetRequiredService<IMyPlayer>();
            IGameNotifier signalRNotifier =
                new Web.Notifiers.Game.SignalRGameNotifier(
                    sp.GetRequiredService<IHubContext<GameHub>>(),
                    logger,
                    sp.GetRequiredService<App.Web.Security.IGamePlayerMappingStore>());

            IGameNotifier actionNotifier = new ActionGameNotifier(gameStartedAfterMatchmakingAction:
                (matchmakingId, gameId, playersMapping) =>
                {
                    logger.Info($"# Gra rozpoczęta po matchmakingu: {matchmakingId} ### {gameId}");

                    if (matchmakingId != myPlayer.GetMatchmakingId()) return;
                    var myMatchmakingPlayerId = myPlayer.GetMatchmakingPlayerId();
                    if (myMatchmakingPlayerId is null) throw new Exception("My player id is null");
                    var gamePlayerId = playersMapping[myMatchmakingPlayerId.Value];
                    
                    myPlayer.SetGameId(gameId);
                    myPlayer.SetGamePlayerId(gamePlayerId);

                    logger.Info("Nasza gra się rozpoczęła");
                    var competitionViewProcess = new ProcessStartInfo()
                    {
                        FileName = "gnome-terminal",
                        Arguments =
                            $"-- bash -c \"dotnet run --project /home/konrad/programming-projects/real_apps/sj_draft/Game/Playground.Game.CompetitionView/Playground.Game.CompetitionView.csproj -- {
                                gameId}; exec bash\"",
                        UseShellExecute = true
                    };
                    var draftConsoleProcess = new ProcessStartInfo()
                    {
                        FileName = "gnome-terminal",
                        Arguments =
                            $"-- bash -c \"dotnet run --project /home/konrad/programming-projects/real_apps/sj_draft/Game/Playground.Game.DraftConsole/Playground.Game.DraftConsole.csproj -- {
                                gameId} {gamePlayerId} http://localhost:5150; exec bash\"",
                        UseShellExecute = true
                    };

                    Process.Start(competitionViewProcess);
                    Process.Start(draftConsoleProcess);
                });

            return new ComposeGameNotifier([signalRNotifier, actionNotifier]);
        });

        return services;
    }
}
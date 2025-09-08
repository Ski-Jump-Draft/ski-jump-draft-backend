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
                new Web.Notifiers.Game.SignalRGameNotifier(sp.GetRequiredService<IHubContext<GameHub>>(), logger);

            IGameNotifier actionNotifier = new ActionGameNotifier(gameStartedAfterMatchmakingAction:
                (matchmakingId, gameId) =>
                {
                    logger.Info($"# Gra rozpoczęta po matchmakingu: {matchmakingId} ### {gameId}");

                    if (matchmakingId != myPlayer.GetMatchmakingId()) return;

                    logger.Info("Nasza gra się rozpoczęła");
                    var psi = new ProcessStartInfo()
                    {
                        FileName = "gnome-terminal",
                        Arguments =
                            $"-- bash -c \"dotnet run --project /home/konrad/programming-projects/real_apps/sj_draft/Game/Playground.Game.CompetitionView/Playground.Game.CompetitionView.csproj -- {
                                gameId}; exec bash\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                });

            return new ComposeGameNotifier([signalRNotifier, actionNotifier]);
        });

        return services;
    }
}
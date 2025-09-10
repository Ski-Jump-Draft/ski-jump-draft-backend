using System.Diagnostics;
using App.Application.Messaging.Notifiers;
using App.Application.OfflineTests;
using App.Application.Utility;
using App.Web.Notifiers.SseHub;
using App.Web.SignalR.Hub;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.DependencyInjection.Production;

public static class Notifiers
{
    public static IServiceCollection AddProductionNotifiers(this IServiceCollection services)
    {
        services.AddSingleton<IMatchmakingNotifier, Web.Notifiers.Matchmaking.Sse>();
        services.AddSingleton<ISseHub, App.Web.Notifiers.SseHub.Default>();

        services.AddSingleton<IGameNotifier, App.Application.Messaging.Notifiers.ComposeGameNotifier>(sp =>
        {
            var logger = sp.GetRequiredService<IMyLogger>();
            IGameNotifier signalRNotifier =
                new Web.Notifiers.Game.SignalRGameNotifier(sp.GetRequiredService<IHubContext<GameHub>>(), logger);

            return new ComposeGameNotifier([signalRNotifier]);
        });

        return services;
    }
}
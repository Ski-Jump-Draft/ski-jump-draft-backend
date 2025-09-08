using App.Application.Commanding;
using App.Application.Game.Gate;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Policy.GameGateSelector;
using App.Application.Utility;
using App.Simulator.Simple;
using App.Domain.Simulation;
using App.Infrastructure.Utility.Logger;
using App.Infrastructure.Utility.Random;
using App.Simulator.Mock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using Playground.Game.Bot.Service;
using Playground.Game.Notifier;
using HillModule = App.Domain.Simulation.HillModule;

namespace Playground.Game;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var random = new SystemRandom();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug);
        });

        var logger = new Dotnet(loggerFactory);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((_, services) => { DependencyInjection.ConfigureServices(services); })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        var myPlayerData = host.Services.GetRequiredService<MyPlayerData>();

        var matchmakingNotifier = (LambdaMatchmakingNotifier)host.Services.GetRequiredService<IMatchmakingNotifier>();

        var commandBus = host.Services.GetRequiredService<ICommandBus>();
        var joinResult = await commandBus
            .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(
                new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(myPlayerData.Nick),
                CancellationToken.None);

        Console.WriteLine($"JoinResult: {joinResult}");
        
        await host.RunAsync();

//        await Task.Delay(-1);
    }
}
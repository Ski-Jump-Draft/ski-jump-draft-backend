using App.Application.Commanding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Playground.Game.Bot.Service;

namespace Playground.Game;

public static class Program
{
    public static async Task Main(string[] args)
    {
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

        var commandBus = host.Services.GetRequiredService<ICommandBus>();
        var joinResult = await commandBus
            .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(
                new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(myPlayerData.Nick),
                CancellationToken.None);

        Console.WriteLine($"JoinResult: {joinResult}");
        
        await host.RunAsync();
    }
}
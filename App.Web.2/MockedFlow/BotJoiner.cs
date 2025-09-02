using App.Application._2.Commanding;
using App.Application._2.Utility;
using App.Domain._2.Matchmaking;

namespace App.Web._2.MockedFlow;

public class BotJoiner(IMatchmakings repo, ICommandBus bus, IClock clock, ILogger<BotJoiner> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rnd = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            // wybierz jakieś matchmaking in progress
            var all = await repo.GetInProgress(stoppingToken);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking?.PlayersCount == 4)
            {
                continue;
            }


            var nick = "Bot" + rnd.Next(1000, 9999);
            var cmd = new App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nick);

            try
            {
                var (matchmakingId, correctedNick, playerId) = await bus
                    .SendAsync<App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                        App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(cmd, stoppingToken);

                log.LogInformation("Bot {correctedNick} joined {Matchmaking}", correctedNick, matchmakingId);
            }
            catch (App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException)
            {
                continue;
                log.LogInformation("Room is full. Bot doesn't join.");
            }
            catch (App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking.MultipleGamesNotSupportedException)
            {
                continue;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Bot failed to join");
            }
        }
    }
}
using App.Application.Commanding;
using App.Application.Utility;
using App.Domain.Matchmaking;
using Microsoft.Extensions.Hosting;

namespace Playground.Game.Bot.Service;

public record MyPlayerData(string Nick);

public class Joiner(IRandom random, MyPlayerData myPlayer, IMatchmakings repo, ICommandBus bus, IClock clock, IMyLogger log, IMyLogger myLogger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var rnd = new Random();

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            // wybierz jakieś matchmaking in progress
            var all = await repo.GetInProgress(ct);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking is null)
            {
                continue;
            }

            var oneSlotRemained = matchmaking.RemainingSlots == 1;
            var myPlayerIsPresent = matchmaking.Players_.Any(player =>
                PlayerModule.NickModule.value(player.Nick) == myPlayer.Nick);
            var needToWaitForMyPlayer = oneSlotRemained && myPlayerIsPresent;

            if (matchmaking.IsFull || needToWaitForMyPlayer)
            {
                continue;
            }

            const string nickBase = "Bot";
            var cmd = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nickBase);

            try
            {
                var (matchmakingId, correctedNick, _) = await bus
                    .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                        App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(cmd, ct);

                log.Debug($"Bot {correctedNick} joined {matchmakingId}");
            }
            catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.RoomIsFullException)
            {
            }
            catch (App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.MultipleGamesNotSupportedException)
            {
            }
            catch (Exception ex)
            {
                log.Error("Bot failed to join", ex);
            }
        }
    }
}
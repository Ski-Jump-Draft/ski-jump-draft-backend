using App.Application.Commanding;
using App.Application.OfflineTests;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Web.HostedServices.MockedFlow;

public class OfflineBotJoiner(
    IMyPlayer myPlayer,
    IMatchmakings repo,
    ICommandBus bus,
    IMyLogger log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            // wybierz jakieś matchmaking in progress
            var all = await repo.GetInProgress(ct);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking is null)
            {
                continue;
            }

            var oneSlotRemained = matchmaking.RemainingSlots == 1;
            var myPlayerIsPresent = matchmaking.Players_.Any(player =>
                PlayerModule.NickModule.value(player.Nick) == myPlayer.GetNick());
            var needToWaitForMyPlayer = oneSlotRemained && myPlayerIsPresent;

            if (matchmaking.IsFull || needToWaitForMyPlayer)
            {
                continue;
            }

            const string nickBase = "Bot";
            var command = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nickBase, IsBot: true);

            try
            {
                var (matchmakingId, correctedNick, playerId) = await bus
                    .SendAsync<App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command,
                        App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Result>(command, ct);

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
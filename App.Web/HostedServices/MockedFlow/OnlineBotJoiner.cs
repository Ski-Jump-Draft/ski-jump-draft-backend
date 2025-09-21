using App.Application.Commanding;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Web.HostedServices.MockedFlow;

public class OnlineBotJoiner(IMatchmakings repo, ICommandBus bus, IMyLogger log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), ct);

            var all = await repo.GetInProgress(ct);
            var matchmaking = all.FirstOrDefault();

            if (matchmaking is null)
            {
                continue;
            }

            var oneSlotRemained = matchmaking.RemainingSlots == 1;

            if (matchmaking.IsFull/* || oneSlotRemained*/)
            {
                continue;
            }

            const string nickBase = "Bot";
            var cmd = new App.Application.UseCase.Matchmaking.JoinQuickMatchmaking.Command(nickBase, IsBot: true);

            try
            {
                var (matchmakingId, correctedNick, playerId) = await bus
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
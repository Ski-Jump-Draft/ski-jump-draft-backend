using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.UseCase.Game.PickByBot;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Service;

public class DraftSystemSchedulerService(
    IScheduler scheduler,
    IClock clock,
    IJson json,
    IBotRegistry botRegistry,
    IMyLogger logger,
    IBotPickLock botPickLock)
{
    public async Task ScheduleSystemDraftEvents(Domain.Game.Game game, CancellationToken ct)
    {
        if (!game.StatusTag.IsDraftTag)
        {
            return;
        }

        var currentTurnInDraft = game.CurrentTurnInDraft.ResultValue.Value;

        MaybeScheduleDraftPassPick(game, currentTurnInDraft, ct).FireAndForget(logger);
        MaybeScheduleBotPick(game, currentTurnInDraft, ct).FireAndForget(logger);
    }

    private async Task<bool> MaybeScheduleDraftPassPick(Domain.Game.Game game, DraftModule.Turn currentTurnInDraft,
        CancellationToken ct)
    {
        var gameId = game.Id.Item;
        var turnIndex = DraftModule.TurnIndexModule.value(currentTurnInDraft.Index);
        var playerId = currentTurnInDraft.PlayerId.Item;
        switch (game.Settings.DraftSettings.TimeoutPolicy)
        {
            case DraftModule.SettingsModule.TimeoutPolicy.TimeoutAfter timeoutAfter:
                var timeoutInSeconds = timeoutAfter.Time.Seconds;
                botPickLock.Unlock(gameId, playerId);
                await scheduler.ScheduleAsync("PassPick",
                    payloadJson: json.Serialize(new
                        { GameId = gameId, PlayerId = playerId, TurnIndex = turnIndex }),
                    runAt: clock.Now().AddSeconds(timeoutInSeconds),
                    uniqueKey: $"PassPick:{gameId}_{playerId}_{
                        turnIndex}", ct: ct);
                return true;
        }

        return false;
    }

    private async Task<bool> MaybeScheduleBotPick(Domain.Game.Game game,
        Domain.Game.DraftModule.Turn currentTurnInDraft,
        CancellationToken ct)
    {
        var gameId = game.Id.Item;
        var turnIndex = DraftModule.TurnIndexModule.value(currentTurnInDraft.Index);
        var playerId = currentTurnInDraft.PlayerId.Item;

        var isBot = botRegistry.IsGameBot(gameId, playerId);
        if (isBot)
        {
            await scheduler.ScheduleAsync("PickByBot",
                payloadJson: json.Serialize(new
                    { GameId = gameId, PlayerId = playerId }),
                runAt: clock.Now(),
                uniqueKey: $"PickByBot:{gameId}_{playerId}_{
                    turnIndex}", ct: ct);

            // await commandBus.SendAsync<UseCase.Game.PickByBot.Command, UseCase.Game.PickByBot.Result>(new Command(gameId, playerId), ct);
        }

        return isBot;
    }
}
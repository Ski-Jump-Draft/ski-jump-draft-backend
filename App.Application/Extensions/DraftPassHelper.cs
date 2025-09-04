using App.Application.Commanding;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.Extensions;

public static class DraftPassHelper
{
    public static async Task<bool> MaybeScheduleDraftPass(App.Domain.Game.Game game, IScheduler scheduler,
        IJson json, IClock clock,
        CancellationToken ct = default)
    {
        if (!game.StatusTag.IsDraftTag)
        {
            return false;
        }

        var gameId = game.Id.Item;
        var currentTurnInDraft = game.CurrentTurnInDraft.ResultValue.Value;

        switch (game.Settings.DraftSettings.TimeoutPolicy)
        {
            case DraftModule.SettingsModule.TimeoutPolicy.TimeoutAfter timeoutAfter:
                var timeoutInSeconds = timeoutAfter.Time.Seconds;
                var turnIndex = DraftModule.TurnIndexModule.value(currentTurnInDraft.Index);
                var playerId = currentTurnInDraft.PlayerId.Item;
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
}
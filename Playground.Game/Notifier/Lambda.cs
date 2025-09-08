using App.Application.Messaging.Notifiers;

namespace Playground.Game.Notifier;

public class LambdaMatchmakingNotifier(
    Action<MatchmakingUpdatedDto> handleMatchmakingUpdate,
    Action<PlayerJoinedDto> handlePlayerJoin,
    Action<PlayerLeftDto> handlePlayerLeft) : IMatchmakingNotifier
{
    public Task MatchmakingUpdated(MatchmakingUpdatedDto matchmaking)
    {
        handleMatchmakingUpdate.Invoke(matchmaking);
        return Task.CompletedTask;
    }

    public Task PlayerJoined(PlayerJoinedDto playerJoined)
    {
        handlePlayerJoin.Invoke(playerJoined);
        return Task.CompletedTask;
    }

    public Task PlayerLeft(PlayerLeftDto playerLeft)
    {
        handlePlayerLeft.Invoke(playerLeft);
        return Task.CompletedTask;
    }
}
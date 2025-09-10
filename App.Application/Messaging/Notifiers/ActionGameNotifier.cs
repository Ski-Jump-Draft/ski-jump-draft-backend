namespace App.Application.Messaging.Notifiers;

public class ActionGameNotifier(
    Action<Guid, Guid, Dictionary<Guid, Guid>>? gameStartedAfterMatchmakingAction = null,
    Action<GameUpdatedDto>? gameUpdatedAction = null,
    Action<Guid>? gameEndedAction = null) : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId,
        Dictionary<Guid, Guid> playersMapping)
    {
        gameStartedAfterMatchmakingAction?.Invoke(matchmakingId, gameId, playersMapping);
        return Task.CompletedTask;
    }

    public Task GameUpdated(GameUpdatedDto matchmaking)
    {
        gameUpdatedAction?.Invoke(matchmaking);
        return Task.CompletedTask;
    }

    public Task GameEnded(Guid gameId)
    {
        gameEndedAction?.Invoke(gameId);
        return Task.CompletedTask;
    }
}
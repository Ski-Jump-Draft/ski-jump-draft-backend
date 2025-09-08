namespace App.Application.Messaging.Notifiers;

public class ComposeGameNotifier(IEnumerable<IGameNotifier> notifiers) : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId)
    {
        foreach (var notifier in notifiers)
        {
            notifier.GameStartedAfterMatchmaking(matchmakingId, gameId);
        }

        return Task.CompletedTask;
    }

    public Task GameUpdated(GameUpdatedDto matchmaking)
    {
        foreach (var notifier in notifiers)
        {
            notifier.GameUpdated(matchmaking);
        }

        return Task.CompletedTask;
    }

    public Task GameEnded(Guid gameId)
    {
        foreach (var notifier in notifiers)
        {
            notifier.GameEnded(gameId);
        }

        return Task.CompletedTask;
    }
}
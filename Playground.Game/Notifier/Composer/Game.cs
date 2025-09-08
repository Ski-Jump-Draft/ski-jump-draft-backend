using System.Diagnostics;
using System.Text.Json;
using App.Application.Messaging.Notifiers;

namespace Playground.Game.Notifier.Composer;

public class ComposerGameNotifier(IEnumerable<IGameNotifier> notifiers) : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId)
    {
        foreach (var gameNotifier in notifiers)
        {
            gameNotifier.GameStartedAfterMatchmaking(matchmakingId, gameId);
        }

        return Task.CompletedTask;
    }

    public Task GameUpdated(GameUpdatedDto matchmaking)
    {
        foreach (var gameNotifier in notifiers)
        {
            gameNotifier.GameUpdated(matchmaking);
        }

        return Task.CompletedTask;
    }

    public Task GameEnded(Guid gameId)
    {
        foreach (var gameNotifier in notifiers)
        {
            gameNotifier.GameEnded(gameId);
        }

        return Task.CompletedTask;
    }
}
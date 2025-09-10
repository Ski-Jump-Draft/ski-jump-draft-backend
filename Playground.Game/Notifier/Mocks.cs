using App.Application.Messaging.Notifiers;

namespace Playground.Game.Notifier;

public class MockGameNotifier : IGameNotifier
{
    public Task GameStartedAfterMatchmaking(Guid matchmakingId, Guid gameId)
    {
        Console.WriteLine($"# Gra rozpoczęta po matchmakingu: {matchmakingId} ### {gameId}");
        return Task.CompletedTask;
    }

    public Task GameUpdated(GameUpdatedDto matchmaking)
    {
        Console.WriteLine($"# Stan gry zaktualizowany:\n{matchmaking}");
        return Task.CompletedTask;
    }

    public Task GameEnded(Guid gameId)
    {
        Console.WriteLine($"# Gra zakończona: {gameId}");
        return Task.CompletedTask;
    }
}
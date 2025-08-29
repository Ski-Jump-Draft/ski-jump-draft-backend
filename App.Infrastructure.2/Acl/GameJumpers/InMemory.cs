using System.Collections.Concurrent;
using App.Application._2.Acl;

namespace App.Infrastructure._2.Acl.GameJumpers;

public class InMemory : IGameJumperAcl
{
    private readonly ConcurrentDictionary<Guid, Guid> _gwToGame = new();
    private readonly ConcurrentDictionary<Guid, Guid> _gameToGw = new();

    public void Map(GameWorldJumperDto gameWorldJumper, GameJumperDto gameJumper)
    {
        if (gameWorldJumper is null || gameJumper is null)
            throw new ArgumentNullException();

        _gwToGame[gameWorldJumper.Id] = gameJumper.Id;
        _gameToGw[gameJumper.Id] = gameWorldJumper.Id;
    }

    public GameJumperDto GetGameJumper(Guid gameWorldJumperId) =>
        _gwToGame.TryGetValue(gameWorldJumperId, out var gameId)
            ? new GameJumperDto(gameId)
            : throw new KeyNotFoundException($"No mapping for GameWorldJumper {gameWorldJumperId}");

    public GameWorldJumperDto GetGameWorldJumper(Guid gameJumperId) =>
        _gameToGw.TryGetValue(gameJumperId, out var gwId)
            ? new GameWorldJumperDto(gwId)
            : throw new KeyNotFoundException($"No mapping for GameJumper {gameJumperId}");
}
using System.Collections.Concurrent;
using App.Application.Acl;

namespace App.Infrastructure.Acl.GameJumpers;

public class InMemory : IGameJumperAcl
{
    private readonly ConcurrentDictionary<(Guid GameId, Guid GameWorldJumperId), Guid> _gameWorldToGame = new();
    private readonly ConcurrentDictionary<Guid, (Guid GameId, Guid GameWorldJumperId)> _gameToGameWorld = new();

    public void Map(GameWorldJumperDto gameWorldJumper, GameJumperDto gameJumper)
    {
        if (gameWorldJumper is null || gameJumper is null)
            throw new ArgumentNullException();

        _gameWorldToGame[(gameJumper.GameId, gameWorldJumper.GameWorldJumperId)] = gameJumper.GameJumperId;
        _gameToGameWorld[gameJumper.GameJumperId] = (gameJumper.GameId, gameWorldJumper.GameWorldJumperId);
    }

    public GameJumperDto GetGameJumper(Guid gameId, Guid gameWorldJumperId) =>
        _gameWorldToGame.TryGetValue((gameId, gameWorldJumperId), out var gameJumperId)
            ? new GameJumperDto(gameId, gameJumperId)
            : throw new KeyNotFoundException($"No mapping for Game {gameId}, GameWorldJumper {gameWorldJumperId}");

    public GameWorldJumperDto GetGameWorldJumper(Guid gameJumperId) =>
        _gameToGameWorld.TryGetValue(gameJumperId, out var value)
            ? new GameWorldJumperDto(value.GameWorldJumperId)
            : throw new KeyNotFoundException($"No mapping for GameJumper {gameJumperId}");
}

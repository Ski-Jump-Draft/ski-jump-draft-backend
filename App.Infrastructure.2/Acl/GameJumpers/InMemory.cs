using System.Collections.Concurrent;
using App.Application._2.Acl;

namespace App.Infrastructure._2.Acl.GameJumpers;

public class InMemory : IGameJumperAcl
{
    private readonly ConcurrentDictionary<(Guid GameId, Guid GameWorldJumperId), GameJumperDto> _map 
        = new();

    public void Map(Guid gameId, Guid gameWorldJumperId, GameJumperDto gameJumper)
    {
        _map[(gameId, gameWorldJumperId)] = gameJumper;
    }

    public GameJumperDto GetGameJumper(Guid gameId, Guid gameWorldJumperId)
    {
        if (_map.TryGetValue((gameId, gameWorldJumperId), out var dto))
            return dto;

        throw new KeyNotFoundException($"Game jumper not found for GameId={gameId}, JumperId={gameWorldJumperId}");
    }
}
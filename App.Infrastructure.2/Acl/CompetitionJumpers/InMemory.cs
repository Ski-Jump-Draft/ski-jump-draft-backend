using System.Collections.Concurrent;
using App.Application._2.Acl;

namespace App.Infrastructure._2.Acl.CompetitionJumpers;

public class InMemory : ICompetitionJumperAcl
{
    private readonly ConcurrentDictionary<(Guid GameId, Guid GameJumperId), CompetitionJumperDto> _map 
        = new();

    public void Map(Guid gameId, Guid gameJumperId, CompetitionJumperDto competitionJumper)
    {
        _map[(gameId, gameJumperId)] = competitionJumper;
    }

    public CompetitionJumperDto GetCompetitionJumper(Guid gameId, Guid gameJumperId)
    {
        if (_map.TryGetValue((gameId, gameJumperId), out var dto))
            return dto;

        throw new KeyNotFoundException($"Competition jumper not found for GameId={gameId}, JumperId={gameJumperId}");
    }
}
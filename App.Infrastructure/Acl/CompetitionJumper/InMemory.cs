using System.Collections.Concurrent;
using App.Application.Acl;

namespace App.Infrastructure.Acl.CompetitionJumper;

public class InMemory : ICompetitionJumperAcl
{
    private readonly ConcurrentDictionary<(Guid GameId, Guid GameJumperId), Guid> _gameToComp = new();
    private readonly ConcurrentDictionary<Guid, (Guid GameId, Guid GameJumperId)> _compToGame = new();

    public void Map(GameJumperDto gameJumper, CompetitionJumperDto competitionJumper)
    {
        if (gameJumper is null || competitionJumper is null)
            throw new ArgumentNullException();

        _gameToComp[(gameJumper.GameId, gameJumper.GameJumperId)] = competitionJumper.Id;
        _compToGame[competitionJumper.Id] = (gameJumper.GameId, gameJumper.GameJumperId);
    }
    public CompetitionJumperDto GetCompetitionJumper(Guid gameId, Guid gameJumperId) =>
        _gameToComp.TryGetValue((gameId, gameJumperId), out var compId)
            ? new CompetitionJumperDto(compId)
            : throw new KeyNotFoundException($"No mapping for Game {gameId}, GameJumper {gameJumperId}");

    public GameJumperDto GetGameJumper(Guid gameId, Guid competitionJumperId) =>
        _compToGame.TryGetValue(competitionJumperId, out var val)
            ? new GameJumperDto(val.GameId, val.GameJumperId)
            : throw new KeyNotFoundException($"No mapping for CompetitionJumper {competitionJumperId} in Game {gameId}");
}

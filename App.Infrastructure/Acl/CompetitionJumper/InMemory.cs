using System.Collections.Concurrent;
using App.Application.Acl;

namespace App.Infrastructure.Acl.CompetitionJumper;

public class InMemory : ICompetitionJumperAcl
{
    private readonly ConcurrentDictionary<Guid, Guid> _gameToComp = new();
    private readonly ConcurrentDictionary<Guid, Guid> _compToGame = new();

    public void Map(GameJumperDto gameJumper, CompetitionJumperDto competitionJumper)
    {
        if (gameJumper is null || competitionJumper is null)
            throw new ArgumentNullException();

        _gameToComp[gameJumper.Id] = competitionJumper.Id;
        _compToGame[competitionJumper.Id] = gameJumper.Id;
    }

    public CompetitionJumperDto GetCompetitionJumper(Guid gameJumperId) =>
        _gameToComp.TryGetValue(gameJumperId, out var compId)
            ? new CompetitionJumperDto(compId)
            : throw new KeyNotFoundException($"No mapping for GameJumper {gameJumperId}");

    public GameJumperDto GetGameJumper(Guid competitionJumperId) =>
        _compToGame.TryGetValue(competitionJumperId, out var gameId)
            ? new GameJumperDto(gameId)
            : throw new KeyNotFoundException($"No mapping for CompetitionJumper {competitionJumperId}");
}
using System.Collections.Concurrent;
using App.Application._2.Acl;

namespace App.Infrastructure._2.Acl.CompetitionHill;

public class InMemory : ICompetitionHillAcl
{
    private readonly ConcurrentDictionary<Guid, Guid> _compToGw = new();
    private readonly ConcurrentDictionary<Guid, Guid> _gwToComp = new();

    public void Map(CompetitionHillDto competitionHill, GameWorldHillDto gameWorldHill)
    {
        if (competitionHill is null || gameWorldHill is null)
            throw new ArgumentNullException();

        _compToGw[competitionHill.Id] = gameWorldHill.Id;
        _gwToComp[gameWorldHill.Id] = competitionHill.Id;
    }

    public GameWorldHillDto GetGameWorldHill(Guid competitionHillId) =>
        _compToGw.TryGetValue(competitionHillId, out var gameWorldHillId)
            ? new GameWorldHillDto(gameWorldHillId)
            : throw new KeyNotFoundException($"No mapping for CompetitionHill {competitionHillId}");

    public CompetitionHillDto GetCompetitionHill(Guid gameWorldHillId) =>
        _gwToComp.TryGetValue(gameWorldHillId, out var competitionHillId)
            ? new CompetitionHillDto(competitionHillId)
            : throw new KeyNotFoundException($"No mapping for GameWorldHill {gameWorldHillId}");
}
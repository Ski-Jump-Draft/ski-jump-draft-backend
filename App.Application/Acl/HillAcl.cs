namespace App.Application.Acl;

public interface ICompetitionHillAcl
{
    void Map(CompetitionHillDto competitionHill, GameWorldHillDto gameWorldHill);
    GameWorldHillDto GetGameWorldHill(Guid competitionHillId);
    CompetitionHillDto GetCompetitionHill(Guid gameWorldHillId);
}

public record GameWorldHillDto(Guid Id);
public record CompetitionHillDto(Guid Id);
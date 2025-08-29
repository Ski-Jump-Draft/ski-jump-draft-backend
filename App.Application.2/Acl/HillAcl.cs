namespace App.Application._2.Acl;

public interface ICompetitionHillAcl
{
    void Map(CompetitionHillDto competitionHill, GameWorldHillDto gameWorldHill);
    GameWorldHillDto GetGameWorldHill(Guid competitionHillId);
    CompetitionHillDto GetCompetitionHill(Guid gameWorldHillId);
}

public record GameWorldHillDto(Guid Id, string Name, string Location, Guid CountryId, string CountryFisCode, int KPoint, int HsPoint);
public record CompetitionHillDto(Guid Id);
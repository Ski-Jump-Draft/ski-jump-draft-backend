namespace App.Application._2.Acl;

public interface IGameJumperAcl
{
    void Map(Guid gameId, Guid gameWorldJumperId, GameJumperDto gameJumper);
    GameJumperDto GetGameJumper(Guid gameId, Guid gameWorldJumperId);
}

public interface ICompetitionJumperAcl
{
    void Map(Guid gameId, Guid gameJumperId, CompetitionJumperDto competitionJumper);
    CompetitionJumperDto GetCompetitionJumper(Guid gameId, Guid gameJumperId);
}

public record GameJumperDto(Guid Id);

public record CompetitionJumperDto(Guid Id);
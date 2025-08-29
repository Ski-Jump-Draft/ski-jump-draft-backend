namespace App.Application._2.Acl;

public interface IGameJumperAcl
{
    void Map(GameWorldJumperDto gameWorldJumper, GameJumperDto gameJumper);
    GameJumperDto GetGameJumper(Guid gameWorldJumperId);
    GameWorldJumperDto GetGameWorldJumper(Guid gameJumperId);
}

public interface ICompetitionJumperAcl
{
    void Map(GameJumperDto gameJumper, CompetitionJumperDto competitionJumper);
    CompetitionJumperDto GetCompetitionJumper(Guid gameJumperId);
    GameJumperDto GetGameJumper(Guid competitionJumperId);
}

public record GameWorldJumperDto(Guid Id);

public record GameJumperDto(Guid Id);

public record CompetitionJumperDto(Guid Id);
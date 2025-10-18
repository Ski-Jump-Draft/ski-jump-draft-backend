namespace App.Application.Acl;

public interface IGameJumperAcl
{
    void Map(GameWorldJumperDto gameWorldJumper, GameJumperDto gameJumper);
    GameJumperDto GetGameJumper(Guid gameId, Guid gameWorldJumperId);
    GameWorldJumperDto GetGameWorldJumper(Guid gameJumperId);
}

public interface ICompetitionJumperAcl
{
    void Map(GameJumperDto gameJumper, CompetitionJumperDto competitionJumper);
    CompetitionJumperDto GetCompetitionJumper(Guid gameId, Guid gameJumperId);
    GameJumperDto GetGameJumper(Guid gameId, Guid competitionJumperId);
}

public record GameWorldJumperDto(Guid GameWorldJumperId);

public record GameJumperDto(Guid GameId, Guid GameJumperId);

public record CompetitionJumperDto(Guid Id);
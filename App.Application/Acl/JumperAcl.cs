namespace App.Application.Acl;

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

public static class JumperAclExtensions
{
    public static IEnumerable<GameWorldJumperDto> ToGameWorldJumpers(
        this Domain.Game.Jumpers gameJumpers, IGameJumperAcl acl)
    {
        var idsEnumerable = Domain.Game.JumpersModule.toIdsList(gameJumpers);
        return idsEnumerable.Select(gameJumperId =>
        {
            var gameWorldJumperDto = acl.GetGameWorldJumper(gameJumperId.Item);
            return new GameWorldJumperDto(gameWorldJumperDto.Id);
        });
    }

    public static IEnumerable<Domain.Competition.Jumper> ToCompetitionJumpers(
        this Domain.Game.Jumpers gameJumpers, ICompetitionJumperAcl acl)
    {
        var idsEnumerable = Domain.Game.JumpersModule.toIdsList(gameJumpers);
        return idsEnumerable.Select(gameJumperId =>
        {
            var competitionJumperDto = acl.GetCompetitionJumper(gameJumperId.Item);
            var competitionJumperId = Domain.Competition.JumperId.NewJumperId(competitionJumperDto.Id);
            return new Domain.Competition.Jumper(competitionJumperId);
        });
    }
}
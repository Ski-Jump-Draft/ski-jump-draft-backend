namespace App.Application.Bot;

public interface IBotRegistry
{
    void RegisterMatchmakingBot(Guid matchmakingId, Guid playerId);
    void RegisterGameBot(Guid gameId, Guid playerId);
    IReadOnlyList<MatchmakingBotDto> MatchmakingBots(Guid matchmakingId);
    IReadOnlyList<GameBotDto> GameBots(Guid matchmakingId);
    int MatchmakingBotsCount(Guid matchmakingId);
    int GameBotsCount(Guid gameId);
    bool IsMatchmakingBot(Guid matchmakingId, Guid playerId);
    bool IsGameBot(Guid gameId, Guid playerId);
}

public record MatchmakingBotDto(Guid PlayerId);
public record GameBotDto(Guid PlayerId);
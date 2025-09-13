namespace App.Application.Bot;

public interface IBotRegistry
{
    void RegisterMatchmakingBot(Guid matchmakingId, Guid playerId);
    void RegisterGameBot(Guid gameId, Guid playerId);
    IReadOnlyList<MatchmakingBotDto> MatchmakingBots(Guid matchmakingId);
    IReadOnlyList<MatchmakingBotDto> GameBots(Guid matchmakingId);
    bool IsMatchmakingBot(Guid matchmakingId, Guid playerId);
    bool IsGameBot(Guid gameId, Guid playerId);
}

public record MatchmakingBotDto(Guid PlayerId);
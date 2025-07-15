namespace App.Application.Projection;

// TODO: Rozbić na wiele projekcji
public interface IGamesProjection
{
    Task<IEnumerable<GameDto>> GetActiveGamesAsync();
    Task<GameMatchmakingDto?> GetGameMatchmakingInfo(Guid gameId);
}

public enum GamePhase
{
    SettingUp,
    Matchmaking,
    PreDraft,
    Draft,
    Competition,
    Ended,
    Break
}

public record GameDto(Guid GameId, GamePhase Phase, DateTimeOffset CreatedAt);
public record GameMatchmakingDto(Guid GameId, int CurrentPlayersCount, int MaxPlayersCount);
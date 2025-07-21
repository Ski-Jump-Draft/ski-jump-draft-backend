namespace App.Application.Projection;

// TODO: Rozbić na wiele projekcji
public interface IActiveGamesProjection
{
    Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct);
    Task<ActiveGameDto?> GetActiveGameAsync(Guid gameId, CancellationToken ct);
}

public enum GamePhase
{
    // SettingUp,
    // Matchmaking,
    PreDraft,
    Draft,
    Competition,
    Ended,
    Break
}

public record ActiveGameDto(Guid GameId, GamePhase Phase, DateTimeOffset CreatedAt);
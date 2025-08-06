namespace App.Application.ReadModel.Projection;

// TODO: Rozbić na wiele projekcji
public interface IActiveGamesProjection
{
    Task<IEnumerable<ActiveGameDto>> GetActiveGamesAsync(CancellationToken ct);
    Task<ActiveGameDto?> GetByIdAsync(Domain.Game.Id.Id gameId, CancellationToken ct);
    Task<ActiveGameTimeLimitsDto?> GetTimeLimitsByIdAsync(Domain.Game.Id.Id gameId, CancellationToken ct);
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

/// <summary>
/// Null break time indicates that it's decided by the host
/// </summary>
/// <param name="GameId"></param>
/// <param name="BreakBeforePreDraft"></param>
/// <param name="BreakBeforeDraft"></param>
/// <param name="BreakBeforeCompetition"></param>
/// <param name="BreakBeforeEnding"></param>
public record ActiveGameTimeLimitsDto(Guid GameId, TimeSpan? BreakBeforePreDraft, TimeSpan? BreakBeforeDraft, TimeSpan? BreakBeforeCompetition, TimeSpan? BreakBeforeEnding);
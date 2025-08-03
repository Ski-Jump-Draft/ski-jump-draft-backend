namespace App.Application.ReadModel.Projection;

public interface IActiveMatchmakingsProjection
{
    Task<IEnumerable<ActiveMatchmakingDto>> GetActiveMatchmakingsAsync(CancellationToken ct);
    Task<ActiveMatchmakingDto?> GetActiveMatchmakingAsync(Guid matchmakingId, CancellationToken ct);
}

public record ActiveMatchmakingDto(Guid MatchmakingId, int CurrentPlayersCount,int MinPlayersCount, int MaxPlayersCount);
namespace App.Application.Projection;

public interface IGameByDraftProjection
{
    Task<GameByDraftDto?> GetGameByDraftIdAsync(Guid draftId, CancellationToken ct);
}

public record GameByDraftDto(Guid GameId, Guid DraftId);
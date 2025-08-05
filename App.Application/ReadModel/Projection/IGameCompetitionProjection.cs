namespace App.Application.ReadModel.Projection;

public interface IGameCompetitionProjection
{
    Task<GameCompetitionDto?> GetActiveCompetitionByGameIdAsync(Domain.Game.Id.Id gameId);
}

public enum GameCompetitionType
{
    PreDraft,
    PostDraft
}

public record GameCompetitionDto(Guid GameId, GameCompetitionType CompetitionType, Guid CompetitionId);
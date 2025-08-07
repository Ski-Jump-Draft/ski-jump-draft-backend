namespace App.Application.ReadModel.Projection;

public interface IGameCompetitionProjection
{
    /// <summary>
    /// Return the current game's competition if a competition is being played.
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    Task<GameCompetitionDto?> GetActiveCompetitionByGameIdAsync(Domain.Game.Id.Id gameId);

    /// <summary>
    /// Returns the PostDraft Competition if it'd been played
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    Task<GamePostDraftCompetitionDto?> GetPostDraftCompetitionByGameIdAsync(Domain.Game.Id.Id gameId);

    /// <summary>
    /// Returns the info about Draft Subjects' position on the PostDraft Competition indicated by gameId
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    Task<GameCompetitionDraftSubjectPositionsDto?> GetDraftSubjectPositionsByEndedGameIdAsync(Domain.Game.Id.Id gameId);
}

public enum GameCompetitionType
{
    PreDraft,
    PostDraft
}

public record GameCompetitionDto(
    Domain.Game.Id.Id GameId,
    GameCompetitionType CompetitionType,
    Domain.SimpleCompetition.CompetitionId CompetitionId);

public record GamePostDraftCompetitionDto(
    Domain.Game.Id.Id GameId,
    Domain.SimpleCompetition.CompetitionId CompetitionId);

public record GameCompetitionDraftSubjectPositionsDto(
    Domain.Game.Id.Id GameId,
    Domain.SimpleCompetition.CompetitionId CompetitionId,
    Dictionary<Domain.Draft.Subject.Id, int> SubjectPositions
);
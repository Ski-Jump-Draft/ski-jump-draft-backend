namespace App.Application.ReadModel.Projection;

public interface IGameDraftProjection
{
    Task<GameEndedDraftDto?> GetEndedDraftByGameIdAsync(Domain.Game.Id.Id gameId);   
}

public record GameEndedDraftDto(Domain.Game.Id.Id GameId, Domain.Draft.Id.Id DraftId, Dictionary<Domain.Game.Participant.Id, IEnumerable<Domain.Draft.Subject.Id>> SubjectsByGameParticipantId);
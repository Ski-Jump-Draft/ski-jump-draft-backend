namespace App.Application.ReadModel.Projection;

public interface IGameParticipantsProjection
{
    Task<IEnumerable<GameParticipantDto>> GetParticipantsByGameIdAsync(
        Domain.Game.Id.Id gameId);
}

public record GameParticipantDto(Guid Id, string Nick);
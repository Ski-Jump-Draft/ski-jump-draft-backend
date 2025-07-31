namespace App.Application.ReadModel.Projection;

public interface IMatchmakingParticipantsProjection
{
    Task<IEnumerable<MatchmakingParticipantDto>> GetParticipantsByMatchmakingIdAsync(
        App.Domain.Matchmaking.Id matchmakingId);
}

public record MatchmakingParticipantDto(Guid Id, string Nick);
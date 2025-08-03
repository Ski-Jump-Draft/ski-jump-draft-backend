namespace App.Application.ReadModel.Projection;

public interface IMatchmakingParticipantsProjection
{
    Task<MatchmakingParticipantDto?> GetParticipantById(Domain.Matchmaking.ParticipantModule.Id id);

    Task<IEnumerable<MatchmakingParticipantDto>> GetParticipantsByMatchmakingIdAsync(
        Domain.Matchmaking.Id matchmakingId);
}

public record MatchmakingParticipantDto(Guid Id, string Nick);
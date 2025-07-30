namespace App.Application.UseCase.Helper;

public interface IGameParticipantFactory
{
    Domain.Game.Participant.Participant Create(Domain.Matchmaking.Participant matchmakingParticipant);
    Domain.Game.Participant.Participant CreateFromDto(Application.ReadModel.Projection.MatchmakingParticipantDto dto);
}
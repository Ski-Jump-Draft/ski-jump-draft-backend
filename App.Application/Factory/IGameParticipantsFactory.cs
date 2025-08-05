namespace App.Application.UseCase.Helper;

public interface IGameParticipantsFactory
{
    // IEnumerable<Domain.Game.Participant.Participant> Create(
    //     IEnumerable<Domain.Matchmaking.Participant> matchmakingParticipant);

    IEnumerable<Domain.Game.Participant.Participant> CreateFromDto(
        IEnumerable<Application.ReadModel.Projection.MatchmakingParticipantDto> matchmakingParticipantDtos);
}
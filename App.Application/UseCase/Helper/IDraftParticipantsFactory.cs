namespace App.Application.UseCase.Helper;

public interface IDraftParticipantsFactory
{
    IEnumerable<Domain.Draft.Participant.Participant> Create(
        Domain.Game.Participant.Participants gameParticipants);

    // TODO: Robić z projekcji, nie encji (?)
    //
    // IEnumerable<Domain.Game.Participant.Participant> CreateFromDto(
    //     IEnumerable<Application.ReadModel.Projection> matchmakingParticipantDtos);
}
namespace App.Application.UseCase.Helper;

public interface IDraftParticipantsFactory
{
    IEnumerable<Domain.Draft.Participant.Participant> CreateFromDtos(
        IEnumerable<ReadModel.Projection.GameParticipantDto> gameParticipantDtos);
}
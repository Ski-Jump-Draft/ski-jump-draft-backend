using App.Application.Commanding.Mappers;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Shared;

namespace App.Application.CompetitionEngine.Impl.GameParticipant;

public class Default(IGuid guid) : IGameParticipantsFactory
{
    public IEnumerable<Participant.Participant> CreateFromDto(
        IEnumerable<MatchmakingParticipantDto> matchmakingParticipantDtos)
    {
        return matchmakingParticipantDtos.Select(dto =>
            new Participant.Participant(Participant.Id.NewId(guid.NewGuid()),
                Participant.NickModule.tryCreate(dto.Nick).ResultValue));
    }
}
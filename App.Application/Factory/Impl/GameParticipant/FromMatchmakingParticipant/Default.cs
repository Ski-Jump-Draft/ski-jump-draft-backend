using App.Application.Abstractions.Mappers;
using App.Application.ReadModel.Projection;
using App.Domain.Game;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.GameParticipant.FromMatchmakingParticipant;

public class Default(IGuid guid) : IValueMapper<MatchmakingParticipantDto, Domain.Game.Participant.Participant>
{
    public Participant.Participant Map(MatchmakingParticipantDto dto)
    {
        var id = Participant.Id.NewId(guid.NewGuid());
        return new Participant.Participant(id);
    }
}
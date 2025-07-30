using App.Application.Abstractions.Mappers;
using App.Application.ReadModel.Projection;
using App.Application.UseCase.Helper;
using App.Domain.Game;
using App.Domain.Shared;

namespace App.Application.Factory.Impl.GameParticipant;

public class Default(IGuid guid) : IGameParticipantFactory
{
    public Participant.Participant Create(Domain.Matchmaking.Participant matchmakingParticipant)
    {
        return new Participant.Participant(Participant.Id.NewId(guid.NewGuid()));
    }

    public Participant.Participant CreateFromDto(MatchmakingParticipantDto dto)
    {
        return new Participant.Participant(Participant.Id.NewId(guid.NewGuid()));
    }
}
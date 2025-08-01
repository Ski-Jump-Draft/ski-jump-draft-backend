using App.Application.UseCase.Helper;
using App.Domain.Matchmaking;
using App.Domain.Shared;

namespace App.Application.CompetitionEngine.Impl.MatchmakingParticipant;

public class Default(IGuid guid) : IMatchmakingParticipantFactory
{
    public Participant CreateFromNick(string nick)
    {
        var participantNick = ParticipantModule.NickModule.tryCreate(nick).ResultValue;

        return new Participant(ParticipantModule.Id.NewId(guid.NewGuid()), participantNick);
    }
}
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Game.Exception;

public class MatchmakingParticipantNotInMatchmakingException : System.Exception
{
    public Domain.Matchmaking.Matchmaking Matchmaking { get; }
    public Application.ReadModel.Projection.MatchmakingParticipantDto? Participant { get; }

    public MatchmakingParticipantNotInMatchmakingException(Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto? participant)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantNotInMatchmakingException(string message, Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto? participant) : base(message)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantNotInMatchmakingException(string message, System.Exception inner,
        Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto? participant) : base(message, inner)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }
}
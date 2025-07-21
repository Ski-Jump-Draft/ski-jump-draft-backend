using App.Domain.Matchmaking;

namespace App.Application.UseCase.Game.Exception;

public class MatchmakingParticipantNotInMatchmakingException : System.Exception
{
    public App.Domain.Matchmaking.Matchmaking Matchmaking { get; }
    public Participant Participant { get; }

    public MatchmakingParticipantNotInMatchmakingException(Domain.Matchmaking.Matchmaking matchmaking,
        Participant participant)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantNotInMatchmakingException(string message, Domain.Matchmaking.Matchmaking matchmaking,
        Participant participant) : base(message)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantNotInMatchmakingException(string message, System.Exception inner,
        Domain.Matchmaking.Matchmaking matchmaking, Participant participant) : base(message, inner)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }
}
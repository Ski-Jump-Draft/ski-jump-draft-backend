namespace App.Application.UseCase.Game.Exception;

public class MatchmakingParticipantAlreadyJoinedException : System.Exception
{
    public Domain.Matchmaking.Matchmaking Matchmaking { get; }
    public Domain.Matchmaking.Participant Participant { get; }

    public MatchmakingParticipantAlreadyJoinedException(Domain.Matchmaking.Matchmaking matchmaking,
        App.Domain.Matchmaking.Participant participant)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantAlreadyJoinedException(string message, Domain.Matchmaking.Matchmaking matchmaking,
        App.Domain.Matchmaking.Participant participant) :
        base(message)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }

    public MatchmakingParticipantAlreadyJoinedException(string message, System.Exception inner,
        Domain.Matchmaking.Matchmaking matchmaking,
        App.Domain.Matchmaking.Participant participant) : base(message, inner)
    {
        Matchmaking = matchmaking;
        Participant = participant;
    }
}
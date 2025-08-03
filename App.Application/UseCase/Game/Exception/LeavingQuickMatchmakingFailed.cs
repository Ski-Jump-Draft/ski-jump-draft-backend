using App.Domain.Matchmaking;

namespace App.Application.UseCase.Game.Exception;

public enum LeavingMatchmakingFailReason
{
    ErrorDuringUpdatingMatchmaking,
    Unknown
}

public class LeavingMatchmakingFailedException : System.Exception
{
    public App.Domain.Matchmaking.Matchmaking Matchmaking { get; }
    public Application.ReadModel.Projection.MatchmakingParticipantDto Participant { get; }
    public LeavingMatchmakingFailReason Reason { get; }

    public LeavingMatchmakingFailedException(Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto participant,
        LeavingMatchmakingFailReason reason)
    {
        Matchmaking = matchmaking;
        Participant = participant;
        Reason = reason;
    }

    public LeavingMatchmakingFailedException(string message, Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto participant,
        LeavingMatchmakingFailReason reason) : base(message)
    {
        Matchmaking = matchmaking;
        Participant = participant;
        Reason = reason;
    }

    public LeavingMatchmakingFailedException(string message, System.Exception inner,
        Domain.Matchmaking.Matchmaking matchmaking,
        Application.ReadModel.Projection.MatchmakingParticipantDto participant,
        LeavingMatchmakingFailReason reason) : base(message, inner)
    {
        Matchmaking = matchmaking;
        Participant = participant;
        Reason = reason;
    }
}
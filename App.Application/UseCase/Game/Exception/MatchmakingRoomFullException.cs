namespace App.Application.UseCase.Game.Exception;

public class MatchmakingRoomFullException : InvalidOperationException
{
    public App.Domain.Matchmaking.Matchmaking Matchmaking { get; }

    public MatchmakingRoomFullException(Domain.Matchmaking.Matchmaking matchmaking)
    {
        Matchmaking = matchmaking;
    }

    public MatchmakingRoomFullException(string message, Domain.Matchmaking.Matchmaking matchmaking) : base(message)
    {
        Matchmaking = matchmaking;
    }

    public MatchmakingRoomFullException(string message, System.Exception inner,
        Domain.Matchmaking.Matchmaking matchmaking) : base(message, inner)
    {
        Matchmaking = matchmaking;
    }
}
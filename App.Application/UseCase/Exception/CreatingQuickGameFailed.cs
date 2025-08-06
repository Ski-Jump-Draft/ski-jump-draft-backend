namespace App.Application.UseCase.Game.Exception;

public class CreatingQuickGameFailedException : System.Exception
{
    public Domain.Matchmaking.Id MatchmakingId { get; }

    public CreatingQuickGameFailedException(Domain.Matchmaking.Id matchmakingId)
    {
        MatchmakingId = matchmakingId;
    }

    public CreatingQuickGameFailedException(string message, Domain.Matchmaking.Id matchmakingId) : base(message)
    {
        MatchmakingId = matchmakingId;
    }

    public CreatingQuickGameFailedException(string message, System.Exception inner, Domain.Matchmaking.Id matchmakingId)
        : base(message, inner)
    {
        MatchmakingId = matchmakingId;
    }
}
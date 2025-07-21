namespace App.Application.UseCase.Game.Exception;

public class CreatingQuickGameFailedException : System.Exception
{
    public Domain.Matchmaking.Matchmaking Matchmaking { get; }
    
    public CreatingQuickGameFailedException(Domain.Matchmaking.Matchmaking matchmaking)
    {
        Matchmaking = matchmaking;
    }

    public CreatingQuickGameFailedException(string message, Domain.Matchmaking.Matchmaking matchmaking) : base(message)
    {
        Matchmaking = matchmaking;
    }

    public CreatingQuickGameFailedException(string message, System.Exception inner, Domain.Matchmaking.Matchmaking matchmaking) : base(message, inner)
    {
        Matchmaking = matchmaking;
    }
}
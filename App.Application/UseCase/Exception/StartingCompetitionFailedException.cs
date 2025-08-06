using App.Domain.Game;

namespace App.Application.UseCase.Exception;

public class StartingCompetitionFailedException : System.Exception
{
    private Domain.Game.Id.Id GameId { get; }
    
    public StartingCompetitionFailedException(Id.Id gameId)
    {
        GameId = gameId;
    }

    public StartingCompetitionFailedException(string message, Id.Id gameId) : base(message)
    {
        GameId = gameId;
    }

    public StartingCompetitionFailedException(string message, System.Exception inner, Id.Id gameId) : base(message, inner)
    {
        GameId = gameId;
    }
}
using App.Domain.Game;

namespace App.Application.UseCase.Exception;

public class PreDraftStartingFailedException : System.Exception
{
    private Domain.Game.Id.Id GameId { get; }

    public PreDraftStartingFailedException(Id.Id gameId)
    {
        GameId = gameId;
    }

    public PreDraftStartingFailedException(string message, Id.Id gameId) : base(message)
    {
        GameId = gameId;
    }

    public PreDraftStartingFailedException(string message, System.Exception inner, Id.Id gameId) : base(message, inner)
    {
        GameId = gameId;
    }
}
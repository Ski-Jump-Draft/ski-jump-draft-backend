using App.Domain.Game;

namespace App.Application.UseCase.Game.Exception;

public class DraftCreationFailedException : System.Exception
{
    public Domain.Game.Id.Id GameId { get; }
    public Domain.Draft.Settings.Settings DraftSettings { get; }
    
    public DraftCreationFailedException(Id.Id gameId, Domain.Draft.Settings.Settings draftSettings)
    {
        GameId = gameId;
        DraftSettings = draftSettings;
    }

    public DraftCreationFailedException(string message, Id.Id gameId, Domain.Draft.Settings.Settings draftSettings) : base(message)
    {
        GameId = gameId;
        DraftSettings = draftSettings;
    }

    public DraftCreationFailedException(string message, System.Exception inner, Id.Id gameId, Domain.Draft.Settings.Settings draftSettings) : base(message, inner)
    {
        GameId = gameId;
        DraftSettings = draftSettings;
    }
}
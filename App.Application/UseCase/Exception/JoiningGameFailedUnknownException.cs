namespace App.Application.UseCase.Game.Exception;

public class JoiningGameFailedUnknownException : System.Exception
{
    public App.Domain.Profile.User.Id UserId { get; }
    public App.Domain.Game.Game Game { get; }

    public JoiningGameFailedUnknownException(App.Domain.Profile.User.Id userId, Domain.Game.Game game)
    {
        UserId = userId;
        Game = game;
    }

    public JoiningGameFailedUnknownException(string message, App.Domain.Profile.User.Id userId, Domain.Game.Game game) :
        base(message)
    {
        UserId = userId;
        Game = game;
    }

    public JoiningGameFailedUnknownException(string message, System.Exception inner, App.Domain.Profile.User.Id userId,
        Domain.Game.Game game) : base(message, inner)
    {
        UserId = userId;
        Game = game;
    }
}
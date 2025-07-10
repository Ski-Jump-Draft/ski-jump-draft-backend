namespace App.Application.Game.Exception;

public class PlayerAlreadyJoinedException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    public App.Domain.Profile.User.Id UserId { get; }

    public PlayerAlreadyJoinedException(Domain.Game.Game game, App.Domain.Profile.User.Id userId)
    {
        Game = game;
        UserId = userId;
    }

    public PlayerAlreadyJoinedException(string message, Domain.Game.Game game, App.Domain.Profile.User.Id userId) :
        base(message)
    {
        Game = game;
        UserId = userId;
    }

    public PlayerAlreadyJoinedException(string message, System.Exception inner, Domain.Game.Game game,
        App.Domain.Profile.User.Id userId) : base(message, inner)
    {
        Game = game;
        UserId = userId;
    }
}
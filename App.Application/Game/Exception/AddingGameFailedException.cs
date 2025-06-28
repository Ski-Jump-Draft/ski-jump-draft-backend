namespace App.Application.Game.Exception;

public class AddingGameFailedException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    
    public AddingGameFailedException(Domain.Game.Game game)
    {
        Game = game;
    }

    public AddingGameFailedException(string message, Domain.Game.Game game) : base(message)
    {
        Game = game;
    }

    public AddingGameFailedException(string message, System.Exception inner, Domain.Game.Game game) : base(message, inner)
    {
        Game = game;
    }
}
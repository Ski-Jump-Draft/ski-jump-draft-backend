namespace App.Application.CSharp.Game.Exception;

public class GameFullException : InvalidOperationException
{
    public App.Domain.Game.Game Game { get; }
    
    public GameFullException(Domain.Game.Game game)
    {
        Game = game;
    }

    public GameFullException(string message, Domain.Game.Game game) : base(message)
    {
        Game = game;
    }

    public GameFullException(string message, System.Exception inner, Domain.Game.Game game) : base(message, inner)
    {
        Game = game;
    }
}
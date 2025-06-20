using App.Domain.Player;

namespace App.Application.CSharp.Game.Exception;

public class JoiningGameFailedException : System.Exception
{
    public App.Domain.Player.Player Player { get; }
    public App.Domain.Game.Game Game { get; }
    
    public JoiningGameFailedException(Player player, Domain.Game.Game game)
    {
        Player = player;
        Game = game;
    }

    public JoiningGameFailedException(string message, Player player, Domain.Game.Game game) : base(message)
    {
        Player = player;
        Game = game;
    }

    public JoiningGameFailedException(string message, System.Exception inner, Player player, Domain.Game.Game game) : base(message, inner)
    {
        Player = player;
        Game = game;
    }
}
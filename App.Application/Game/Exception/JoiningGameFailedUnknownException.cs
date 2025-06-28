using App.Domain.Player;

namespace App.Application.Game.Exception;

public class JoiningGameFailedUnknownException : System.Exception
{
    public App.Domain.Player.Player Player { get; }
    public App.Domain.Game.Game Game { get; }
    
    public JoiningGameFailedUnknownException(Player player, Domain.Game.Game game)
    {
        Player = player;
        Game = game;
    }

    public JoiningGameFailedUnknownException(string message, Player player, Domain.Game.Game game) : base(message)
    {
        Player = player;
        Game = game;
    }

    public JoiningGameFailedUnknownException(string message, System.Exception inner, Player player, Domain.Game.Game game) : base(message, inner)
    {
        Player = player;
        Game = game;
    }
}
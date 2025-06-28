using App.Domain.Player;

namespace App.Application.Game.Exception;

public class PlayerAlreadyJoinedException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    public App.Domain.Player.Player Player { get; }

    public PlayerAlreadyJoinedException(Domain.Game.Game game, Player player)
    {
        Game = game;
        Player = player;
    }

    public PlayerAlreadyJoinedException(string message, Domain.Game.Game game, Player player) : base(message)
    {
        Game = game;
        Player = player;
    }

    public PlayerAlreadyJoinedException(string message, System.Exception inner, Domain.Game.Game game, Player player) : base(message, inner)
    {
        Game = game;
        Player = player;
    }
}
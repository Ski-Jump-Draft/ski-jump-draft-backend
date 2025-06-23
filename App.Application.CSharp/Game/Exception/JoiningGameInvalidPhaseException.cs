using App.Domain.Game;

namespace App.Application.CSharp.Game.Exception;

public class JoiningGameInvalidPhaseException : System.Exception
{
    public App.Domain.Game.Game Game { get; }
    public App.Domain.Game.GameModule.Phase Phase { get; }
    
    public JoiningGameInvalidPhaseException(Domain.Game.Game game, GameModule.Phase phase)
    {
        Game = game;
        Phase = phase;
    }

    public JoiningGameInvalidPhaseException(string message, Domain.Game.Game game, GameModule.Phase phase) : base(message)
    {
        Game = game;
        Phase = phase;
    }

    public JoiningGameInvalidPhaseException(string message, System.Exception inner, Domain.Game.Game game, GameModule.Phase phase) : base(message, inner)
    {
        Game = game;
        Phase = phase;
    }
}
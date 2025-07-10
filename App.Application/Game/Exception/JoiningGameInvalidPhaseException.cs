using App.Domain.Game;

namespace App.Application.Game.Exception;

public class JoiningGameInvalidPhaseException : System.Exception
{
    public List<App.Domain.Game.GameModule.PhaseTag> Expected { get; }
    public App.Domain.Game.GameModule.PhaseTag Actual { get; }

    public JoiningGameInvalidPhaseException(List<App.Domain.Game.GameModule.PhaseTag> expected,
        GameModule.PhaseTag actual)
    {
        Expected = expected;
        Actual = actual;
    }

    public JoiningGameInvalidPhaseException(string message, List<App.Domain.Game.GameModule.PhaseTag> expected,
        GameModule.PhaseTag actual) : base(message)
    {
        Expected = expected;
        Actual = actual;
    }

    public JoiningGameInvalidPhaseException(string message, System.Exception inner,
        List<App.Domain.Game.GameModule.PhaseTag> expected, GameModule.PhaseTag actual) : base(message, inner)
    {
        Expected = expected;
        Actual = actual;
    }
}
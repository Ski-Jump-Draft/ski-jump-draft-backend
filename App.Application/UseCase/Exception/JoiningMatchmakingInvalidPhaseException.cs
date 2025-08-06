using App.Domain.Game;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Game.Exception;

public class JoiningMatchmakingInvalidPhaseException : System.Exception
{
    public List<PhaseTag> Expected { get; }
    public PhaseTag Actual { get; }

    public JoiningMatchmakingInvalidPhaseException(List<PhaseTag> expected,
        PhaseTag actual)
    {
        Expected = expected;
        Actual = actual;
    }

    public JoiningMatchmakingInvalidPhaseException(string message, List<PhaseTag> expected,
        PhaseTag actual) : base(message)
    {
        Expected = expected;
        Actual = actual;
    }

    public JoiningMatchmakingInvalidPhaseException(string message, System.Exception inner,
        List<PhaseTag> expected, PhaseTag actual) : base(message, inner)
    {
        Expected = expected;
        Actual = actual;
    }
}
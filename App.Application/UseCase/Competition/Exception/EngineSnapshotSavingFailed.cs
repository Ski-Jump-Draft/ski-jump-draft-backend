namespace App.Application.UseCase.Competition.Exception;

public class EngineSnapshotSavingFailedException : System.Exception
{
    public EngineSnapshotSavingFailedException()
    {
    }

    public EngineSnapshotSavingFailedException(string message) : base(message)
    {
    }

    public EngineSnapshotSavingFailedException(string message, System.Exception inner) : base(message, inner)
    {
    }
}
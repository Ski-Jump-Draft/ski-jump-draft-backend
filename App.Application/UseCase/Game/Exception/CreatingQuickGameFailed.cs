namespace App.Application.UseCase.Game.Exception;

public class CreatingQuickGameFailedException : System.Exception
{
    public CreatingQuickGameFailedException()
    {
    }

    public CreatingQuickGameFailedException(string message) : base(message)
    {
    }

    public CreatingQuickGameFailedException(string message, System.Exception inner) : base(message, inner)
    {
    }
}
namespace App.Application.UseCase.Game.Exception;

public enum Reason
{
    NoServerAvailable,
    GameAlreadyRunning,
    ErrorDuringSettingUpGame,
    ErrorDuringPreservingParticipant,
    Unknown
}

public class JoiningQuickGameFailedException : System.Exception
{
    public string Nick { get; }
    public Reason Reason { get; }
    
    public JoiningQuickGameFailedException(string nick, Reason reason)
    {
        Nick = nick;
        this.Reason = reason;
    }

    public JoiningQuickGameFailedException(string message, string nick, Reason reason) : base(message)
    {
        Nick = nick;
        this.Reason = reason;
    }

    public JoiningQuickGameFailedException(string message, System.Exception inner, string nick, Reason reason) : base(message, inner)
    {
        Nick = nick;
        this.Reason = reason;
    }
}
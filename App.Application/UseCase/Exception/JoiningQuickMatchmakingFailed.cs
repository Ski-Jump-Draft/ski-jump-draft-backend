namespace App.Application.UseCase.Game.Exception;

public enum JoiningQuickMatchmakingFailReason
{
    NoServerAvailable,
    GameAlreadyRunning,
    ErrorDuringSettingUp,
    ErrorDuringPreservingParticipant,
    Unknown
}

public class JoiningQuickMatchmakingFailedException : System.Exception
{
    public string Nick { get; }
    public JoiningQuickMatchmakingFailReason Reason { get; }
    
    public JoiningQuickMatchmakingFailedException(string nick, JoiningQuickMatchmakingFailReason reason)
    {
        Nick = nick;
        Reason = reason;
    }

    public JoiningQuickMatchmakingFailedException(string message, string nick, JoiningQuickMatchmakingFailReason reason) : base(message)
    {
        Nick = nick;
        Reason = reason;
    }

    public JoiningQuickMatchmakingFailedException(string message, System.Exception inner, string nick, JoiningQuickMatchmakingFailReason reason) : base(message, inner)
    {
        Nick = nick;
        Reason = reason;
    }
}
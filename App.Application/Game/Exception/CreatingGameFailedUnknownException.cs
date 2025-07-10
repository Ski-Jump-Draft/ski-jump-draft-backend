using App.Domain.Game.Hosting;

namespace App.Application.Game.Exception;

public class CreatingGameFailedUnknownException : System.Exception
{
    public Host Host { get; }
    public App.Domain.Game.Settings.Settings GameSettings { get; }

    public CreatingGameFailedUnknownException(Host host, Domain.Game.Settings.Settings gameSettings)
    {
        Host = host;
        GameSettings = gameSettings;
    }

    public CreatingGameFailedUnknownException(string message, Host host, Domain.Game.Settings.Settings gameSettings) : base(message)
    {
        Host = host;
        GameSettings = gameSettings;
    }

    public CreatingGameFailedUnknownException(string message, System.Exception inner, Host host, Domain.Game.Settings.Settings gameSettings)
        : base(message, inner)
    {
        Host = host;
        GameSettings = gameSettings;
    }
}
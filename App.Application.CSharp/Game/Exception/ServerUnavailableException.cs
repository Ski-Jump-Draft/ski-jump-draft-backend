using App.Domain.Game;

namespace App.Application.CSharp.Game.Exception;

public class ServerUnavailableException : System.Exception
{
    public App.Domain.Game.Server Server { get; }
    
    public ServerUnavailableException(Server server)
    {
        Server = server;
    }

    public ServerUnavailableException(string message, Server server) : base(message)
    {
        Server = server;
    }

    public ServerUnavailableException(string message, System.Exception inner, Server server) : base(message, inner)
    {
        Server = server;
    }
}
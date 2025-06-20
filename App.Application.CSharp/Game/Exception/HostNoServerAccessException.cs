using App.Domain.Game;

namespace App.Application.CSharp.Game.Exception;

public class HostNoServerAccessException : System.Exception
{
    public App.Domain.Game.Host Host { get; }
    public App.Domain.Game.Server Server { get; }
    
    public HostNoServerAccessException(Host host, Server server)
    {
        Server = server;
        Host = host;
    }

    public HostNoServerAccessException(string message, Host host, Server server) : base(message)
    {
        Server = server;
        Host = host;
    }

    public HostNoServerAccessException(string message, System.Exception inner , Host host, Server server) : base(message, inner)
    {
        Server = server;
        Host = host;
    }
}
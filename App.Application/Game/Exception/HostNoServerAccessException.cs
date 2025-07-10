using App.Domain.Game;
using App.Domain.Game.Hosting;

namespace App.Application.Game.Exception;

public class HostNoServerAccessException : System.Exception
{
    public Host Host { get; }
    public Server Server { get; }

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

    public HostNoServerAccessException(string message, System.Exception inner, Host host, Server server) : base(message,
        inner)
    {
        Server = server;
        Host = host;
    }
}
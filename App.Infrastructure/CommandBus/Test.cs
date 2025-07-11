using App.Application.Abstractions;

namespace App.Infrastructure.CommandBus;

public class TestCommandBus : ICommandBus
{
    private readonly List<object> _commands = [];

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
    {
        _commands.Add(command!);
        return Task.CompletedTask;
    }
    
    public bool WasSent<TCommand>(Func<TCommand, bool> predicate)
    {
        return _commands.OfType<TCommand>().Any(predicate);
    }
}
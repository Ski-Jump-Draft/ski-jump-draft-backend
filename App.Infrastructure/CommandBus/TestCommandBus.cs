using App.Application.Abstractions;

namespace App.Infrastructure.CommandBus;

public class TestCommandBus(IServiceProvider sp) : ICommandBus
{
    Task ICommandBus.SendAsync<TCommand>(TCommand command, CancellationToken ct)
    {
        var handler = (ICommandHandler<TCommand>?)sp.GetService(
                          typeof(ICommandHandler<TCommand>))
                      ?? throw new InvalidOperationException(
                          $"No handler for command {typeof(TCommand).Name}"
                      );
        return handler.HandleAsync(command, ct);
    }

    public Task<TResponse> SendAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken ct
    ) where TCommand : ICommand<TResponse>
    {
        var handler = (ICommandHandler<TCommand, TResponse>?)sp.GetService(
                          typeof(ICommandHandler<TCommand, TResponse>))
                      ?? throw new InvalidOperationException(
                          $"No handler for command {typeof(TCommand).Name}"
                      );
        return handler.HandleAsync(command, ct);
    }
}
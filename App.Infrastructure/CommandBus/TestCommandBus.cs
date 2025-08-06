using App.Application.Commanding;

namespace App.Infrastructure.CommandBus;

public class TestCommandBus(IServiceProvider sp) : ICommandBus
{
    public Task SendAsync<TCommand>(CommandEnvelope<TCommand> envelope, CancellationToken ct)
        where TCommand : ICommand
    {
        var handler = (ICommandHandler<TCommand>?)sp.GetService(typeof(ICommandHandler<TCommand>))
                      ?? throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");
        return handler.HandleAsync(envelope.Command, envelope.MessageContext, ct);
    }

    public Task<TResponse> SendAsync<TCommand, TResponse>(
        CommandEnvelope<TCommand, TResponse> envelope,
        CancellationToken ct
    )
        where TCommand : ICommand<TResponse>
    {
        var handler = (ICommandHandler<TCommand, TResponse>?)sp.GetService(typeof(ICommandHandler<TCommand, TResponse>))
                      ?? throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");
        return handler.HandleAsync(envelope.Command, envelope.Context, ct);
    }
}
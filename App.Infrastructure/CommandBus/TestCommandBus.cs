using App.Application.Commanding;

namespace App.Infrastructure.CommandBus;

public class TestCommandBus(IServiceProvider sp) : ICommandBus
{
    public async Task SendAsync<TCommand>(CommandEnvelope<TCommand> envelope, CancellationToken ct,
        TimeSpan? delay = null)
        where TCommand : ICommand
    {
        if (delay is { TotalMilliseconds: > 0 })
            await Task.Delay(delay.Value, ct).ConfigureAwait(false);
        var handler = (ICommandHandler<TCommand>?)sp.GetService(typeof(ICommandHandler<TCommand>))
                      ?? throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");
        await handler.HandleAsync(envelope.Command, envelope.MessageContext, ct);
    }

    public async Task<TResponse> SendAsync<TCommand, TResponse>(
        CommandEnvelope<TCommand, TResponse> envelope,
        CancellationToken ct, TimeSpan? delay = null
    )
        where TCommand : ICommand<TResponse>
    {
        if (delay is { TotalMilliseconds: > 0 })
            await Task.Delay(delay.Value, ct).ConfigureAwait(false);
        var handler = (ICommandHandler<TCommand, TResponse>?)sp.GetService(typeof(ICommandHandler<TCommand, TResponse>))
                      ?? throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");
        return await handler.HandleAsync(envelope.Command, envelope.Context, ct);
    }
}
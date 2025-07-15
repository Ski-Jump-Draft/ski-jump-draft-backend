using App.Application.Abstractions;

namespace App.Infrastructure.CommandBus;

public class InMemoryCommandBus(IServiceProvider sp) : ICommandBus
{
    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand));
        var handler = sp.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");

        var method = handlerType.GetMethod("HandleAsync");
        var task = (Task)method?.Invoke(handler, [command, ct])!;
        await task.ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken ct
    ) where TCommand : ICommand<TResponse>
    {
        // Znajdź handlera ICommandHandler<TCommand, TResponse>
        // TODO: A nie ICommandHandler<,>
        var handlerType = typeof(ICommandHandler<>)
            .MakeGenericType(typeof(TCommand), typeof(TResponse));
        var handler = sp.GetService(handlerType);
        if (handler == null)
            throw new InvalidOperationException(
                $"No handler for command {typeof(TCommand).Name}"
            );

        // Wywołaj HandleAsync(TCommand, CancellationToken)
        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
            throw new InvalidOperationException(
                $"Handler {handlerType.Name} missing HandleAsync method"
            );

        var resultTask = (Task<TResponse>)method.Invoke(
            handler,
            [command, ct]
        )!;

        return await resultTask.ConfigureAwait(false);
    }

}
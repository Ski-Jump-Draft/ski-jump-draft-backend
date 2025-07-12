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
}
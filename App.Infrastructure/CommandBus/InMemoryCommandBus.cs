using App.Application.Abstractions;

namespace App.Infrastructure.CommandBus;

public class InMemoryCommandBus : ICommandBus
{
    private readonly IServiceProvider _sp;

    public InMemoryCommandBus(IServiceProvider sp)
    {
        _sp = sp;
    }

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand));
        var handler = _sp.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");

        var method = handlerType.GetMethod("HandleAsync");
        var task = (Task)method.Invoke(handler, new object[] { command, ct });
        await task.ConfigureAwait(false);
    }
}

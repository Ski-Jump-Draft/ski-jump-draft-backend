using App.Application.Abstractions;

namespace App.Infrastructure.CommandHandler;

public class InMemoryCommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryCommandBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(typeof(TCommand));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
            throw new InvalidOperationException($"No handler for command {typeof(TCommand).Name}");

        var method = handlerType.GetMethod("HandleAsync");
        if (command != null)
        {
            var task = (Task)method?.Invoke(handler, [command, ct])!;
            await task.ConfigureAwait(false);
        }
    }
}
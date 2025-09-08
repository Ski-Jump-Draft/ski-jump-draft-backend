using App.Application.Commanding;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.Commanding.CommandBus;

public class InMemory : ICommandBus
{
    private readonly IServiceProvider _sp;

    public InMemory(IServiceProvider sp) => _sp = sp;

    public Task SendAsync<TCommand>(
        TCommand command,
        CancellationToken ct) where TCommand : ICommand
    {
        var handler = _sp.GetRequiredService<ICommandHandler<TCommand>>();
        return handler.HandleAsync(command, ct);
    }

    public Task<TResponse> SendAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken ct) where TCommand : ICommand<TResponse>
    {
        var handler = _sp.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return handler.HandleAsync(command, ct);
    }
}
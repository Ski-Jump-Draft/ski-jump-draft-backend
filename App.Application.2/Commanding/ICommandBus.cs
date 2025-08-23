namespace App.Application._2.Commanding;

public interface ICommandBus
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct, TimeSpan? delay = null)
        where TCommand : ICommand;

    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command,
        CancellationToken ct, TimeSpan? delay = null)
        where TCommand : ICommand<TResponse>;
}
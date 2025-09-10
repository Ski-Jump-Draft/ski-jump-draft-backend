namespace App.Application.Commanding;

public interface ICommandBus
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct)
        where TCommand : ICommand;

    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command,
        CancellationToken ct)
        where TCommand : ICommand<TResponse>;
}
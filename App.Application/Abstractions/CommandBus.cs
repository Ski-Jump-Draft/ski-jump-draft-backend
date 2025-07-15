namespace App.Application.Abstractions;

public interface ICommandBus
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct);
    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct) 
        where TCommand : ICommand<TResponse>;
}

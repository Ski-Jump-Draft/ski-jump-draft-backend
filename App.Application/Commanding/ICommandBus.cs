namespace App.Application.Abstractions;

public interface ICommandBus
{
    Task SendAsync<TCommand>(CommandEnvelope<TCommand> envelope, CancellationToken ct)
        where TCommand : ICommand;

    Task<TResponse> SendAsync<TCommand, TResponse>(CommandEnvelope<TCommand, TResponse> envelope,
        CancellationToken ct)
        where TCommand : ICommand<TResponse>;
}
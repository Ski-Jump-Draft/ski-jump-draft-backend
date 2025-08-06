namespace App.Application.Commanding;

public interface ICommandBus
{
    Task SendAsync<TCommand>(CommandEnvelope<TCommand> envelope, CancellationToken ct, TimeSpan? delay = null)
        where TCommand : ICommand;

    Task<TResponse> SendAsync<TCommand, TResponse>(CommandEnvelope<TCommand, TResponse> envelope,
        CancellationToken ct, TimeSpan? delay = null)
        where TCommand : ICommand<TResponse>;
}
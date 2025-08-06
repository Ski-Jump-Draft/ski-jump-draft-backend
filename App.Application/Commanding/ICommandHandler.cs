namespace App.Application.Commanding;

public interface ICommandHandler<in TCommand>
{
    Task HandleAsync(TCommand command, MessageContext messageContext, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, MessageContext messageContext, CancellationToken ct);
}
namespace App.Application.Abstractions;

public interface ICommandHandler<in TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct);
}
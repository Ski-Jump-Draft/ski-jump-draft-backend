namespace App.Application;

public interface IApplicationHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}

public interface IApplicationHandler<TReturn, TCommand>
{
    public Task<TReturn> HandleAsync(TCommand command, CancellationToken ct);
}
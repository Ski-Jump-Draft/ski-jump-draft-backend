namespace App.Application.Abstractions;

public interface ICommandBus
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct);
}

using App.Application.Abstractions;

namespace App.Infrastructure.CommandBus;

// w testach – w folderze Helpers albo tuż w samym pliku:
public class SpyCommandBus : ICommandBus
{
    readonly List<object> _sent = new();

    public Task SendAsync<TCommand>(CommandEnvelope<TCommand> command, CancellationToken ct) where TCommand : ICommand
    {
        _sent.Add(command!);
        return Task.CompletedTask;
    }

    public Task<TResponse> SendAsync<TCommand, TResponse>(
        CommandEnvelope<TCommand, TResponse> command,
        CancellationToken ct
    ) where TCommand : ICommand<TResponse>
    {
        _sent.Add(command!);
        // musimy zwrócić jakąś wartość, ale tutaj akurat EndDraftPhase.Command zwraca bool
        // więc zwracamy `true`, żeby handler nie potknął się o brak wartości
        return Task.FromResult((TResponse)(object)true)!;
    }

    public bool WasSent<TCommand>(Func<TCommand, bool>? predicate = null)
    {
        var any = _sent.OfType<TCommand>();
        return predicate is null
            ? any.Any()
            : any.Any(predicate);
    }
}
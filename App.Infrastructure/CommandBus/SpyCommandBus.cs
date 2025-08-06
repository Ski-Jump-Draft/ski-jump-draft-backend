using App.Application.Commanding;

namespace App.Infrastructure.CommandBus;

// w testach – w folderze Helpers albo tuż w samym pliku:
public class SpyCommandBus : ICommandBus
{
    readonly List<object> _sent = new();

    public async Task SendAsync<TCommand>(CommandEnvelope<TCommand> command, CancellationToken ct,
        TimeSpan? delay = null)
        where TCommand : ICommand
    {
        if (delay is { TotalMilliseconds: > 0 })
            await Task.Delay(delay.Value, ct).ConfigureAwait(false);
        _sent.Add(command!);
    }

    public async Task<TResponse> SendAsync<TCommand, TResponse>(
        CommandEnvelope<TCommand, TResponse> command,
        CancellationToken ct,
        TimeSpan? delay = null
    ) where TCommand : ICommand<TResponse>
    {
        if (delay is { TotalMilliseconds: > 0 })
            await Task.Delay(delay.Value, ct).ConfigureAwait(false);
        _sent.Add(command!);
        return (TResponse)(object)true;
    }

    public bool WasSent<TCommand>(Func<TCommand, bool>? predicate = null)
    {
        var any = _sent.OfType<TCommand>();
        return predicate is null
            ? any.Any()
            : any.Any(predicate);
    }
}
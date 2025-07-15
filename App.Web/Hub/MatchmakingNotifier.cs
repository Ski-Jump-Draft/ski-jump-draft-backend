using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

namespace App.Web.Hub;

public class MatchmakingNotifier(IHubContext<MatchmakingHub> hub)
{
    private readonly ConcurrentDictionary<Guid, Channel<MatchmakingEvent>> _channels = new();

    public async Task NotifyUpdated(string gameId, int current, int max) =>
        await hub.Clients.Group(gameId).SendAsync("updated", new
            { CurrentPlayersCount = current, MaxPlayersCount = max });

    public async Task NotifyEnded(string gameId, int players) =>
        await hub.Clients.Group(gameId).SendAsync("ended", new
            { PlayersCount = players });

    public async Task NotifyFailed(string gameId, int current, int max, string reason) =>
        await hub.Clients.Group(gameId).SendAsync("updated", new
            { PlayersCount = current, MaxPlayersCount = max, reason });
    
    public async IAsyncEnumerable<MatchmakingEvent> Subscribe(
        Guid gameId,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var channel = _channels.GetOrAdd(gameId,
            _ => Channel.CreateUnbounded<MatchmakingEvent>());

        // TODO: Możesz tu jeszcze sprawdzić, czy participantId jest w grze

        await foreach (var ev in channel.Reader.ReadAllAsync(ct))
        {
            yield return ev;
        }
    }
}

public record MatchmakingEvent(string Type, object Data);
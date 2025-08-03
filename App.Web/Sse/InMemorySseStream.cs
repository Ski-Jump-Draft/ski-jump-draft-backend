using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace App.Web.Sse;

public class InMemorySseStream(ILogger<InMemorySseStream> log) : ISseStream
{
    private readonly ConcurrentDictionary<string, HashSet<Channel<string>>> _subs = new();

    public ChannelReader<string> Subscribe(string channel)
    {
        var ch = Channel.CreateUnbounded<string>();
        _subs.AddOrUpdate(
            channel,
            _ => [ch],
            (_, set) =>
            {
                set.Add(ch);
                return set;
            });

        log.LogDebug("SSE subscribe: {Channel}, total subs = {Count}", channel, _subs[channel].Count);
        return ch.Reader;
    }

    public void Unsubscribe(string channel, ChannelReader<string> reader)
    {
        if (!_subs.TryGetValue(channel, out var set)) return;

        // znajdź parę Channel, której Reader pasuje do przekazanego
        var toRemove = set.FirstOrDefault(c => c.Reader == reader);
        if (toRemove is null) return;
        set.Remove(toRemove);
        log.LogDebug("SSE unsubscribe: {Channel}, remaining subs = {Count}", channel, set.Count);
    }

    public Task PublishAsync(string channel, string eventName, object payload, CancellationToken ct = default)
    {
        if (!_subs.TryGetValue(channel, out var set) || set.Count == 0) return Task.CompletedTask;

        var json = JsonSerializer.Serialize(payload);
        var sse = $"event: {eventName}\ndata: {json}\n\n";

        var dead = set.Where(ch => !ch.Writer.TryWrite(sse)).ToList();

        // usuń martwe kanały
        foreach (var d in dead) set.Remove(d);

        log.LogTrace("SSE publish: {Channel} '{Event}' to {Count} subs", channel, eventName, set.Count);
        return Task.CompletedTask;
    }
}
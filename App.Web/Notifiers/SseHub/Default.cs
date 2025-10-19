using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace App.Web.Notifiers.SseHub;

public class Default : ISseHub
{
    private sealed class Client
    {
        public HttpResponse Response { get; }
        public SemaphoreSlim WriteLock { get; } = new(1, 1);
        public CancellationToken Ct { get; }
        public Client(HttpResponse response, CancellationToken ct)
        {
            Response = response;
            Ct = ct;
        }
    }

    private static readonly byte[] HeartbeatBytes = Encoding.UTF8.GetBytes(new[] { ':', '\n', '\n' });

    private readonly ConcurrentDictionary<Guid, ConcurrentBag<Client>> _streams = new();

    public void Subscribe(Guid matchmakingId, HttpResponse response, CancellationToken ct)
    {
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";

        var client = new Client(response, ct);
        var bag = _streams.GetOrAdd(matchmakingId, _ => new ConcurrentBag<Client>());
        bag.Add(client);

        // Heartbeat to prevent idle intermediaries (CDN/proxy) from closing the connection (e.g., ~10s idle)
        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(8), ct);
                        if (ct.IsCancellationRequested) break;
                        await SafeWriteAsync(client, HeartbeatBytes);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // ignore transient write errors; client will likely drop
                    }
                }
            }
            catch
            {
                // swallow background task exceptions
            }
        }, ct);

        ct.Register(() =>
        {
            // Removal from ConcurrentBag is not supported; we accept stale entries.
            // In production, consider a better structure to allow cleanup.
        });
    }

    public async Task PublishAsync(Guid matchmakingId, string eventName, string json, CancellationToken ct)
    {
        if (!_streams.TryGetValue(matchmakingId, out var clients))
            return;

        var data = $"event: {eventName}\\ndata: {json}\\n\\n";
        var buffer = Encoding.UTF8.GetBytes(data);

        foreach (var client in clients)
        {
            if (client.Ct.IsCancellationRequested) continue;
            try
            {
                await SafeWriteAsync(client, buffer);
            }
            catch
            {
                // client likely disconnected
            }
        }
    }

    private static async Task SafeWriteAsync(Client client, byte[] buffer)
    {
        await client.WriteLock.WaitAsync(CancellationToken.None);
        try
        {
            await client.Response.Body.WriteAsync(buffer, CancellationToken.None);
            await client.Response.Body.FlushAsync(CancellationToken.None);
        }
        finally
        {
            client.WriteLock.Release();
        }
    }
}

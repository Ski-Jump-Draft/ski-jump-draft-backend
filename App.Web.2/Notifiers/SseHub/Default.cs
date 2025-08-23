using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace App.Web._2.Notifiers.SseHub;

public class Default : ISseHub
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<HttpResponse>> _streams = new();

    public void Subscribe(Guid matchmakingId, HttpResponse response, CancellationToken ct)
    {
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";

        var bag = _streams.GetOrAdd(matchmakingId, _ => new ConcurrentBag<HttpResponse>());
        bag.Add(response);

        ct.Register(() =>
        {
            // usuwanie klienta — uproszczone, bo ConcurrentBag nie ma Remove
            // w realnym kodzie raczej Channel albo ConcurrentDictionary z markerem
        });
    }

    public async Task PublishAsync(Guid matchmakingId, string eventName, string json, CancellationToken ct)
    {
        if (!_streams.TryGetValue(matchmakingId, out var clients))
            return;

        var data = $"event: {eventName}\ndata: {json}\n\n";
        var buffer = Encoding.UTF8.GetBytes(data);

        foreach (var client in clients)
        {
            try
            {
                await client.Body.WriteAsync(buffer, ct);
                await client.Body.FlushAsync(ct);
            }
            catch { /* klient padł */ }
        }
    }
}

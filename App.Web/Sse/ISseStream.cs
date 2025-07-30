using System.Threading.Channels;

namespace App.Web.Sse;

// Web layer
public interface ISseStream
{
    ChannelReader<string> Subscribe(string channel);   // odbiorca SSE
    Task PublishAsync(string channel, string eventName, object payload, CancellationToken ct = default);
    void Unsubscribe(string channel, ChannelReader<string> reader); // porządkowanie
}
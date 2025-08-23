using Microsoft.AspNetCore.Http;

namespace App.Web._2.Notifiers.SseHub;

public interface ISseHub
{
    void Subscribe(Guid matchmakingId, HttpResponse response, CancellationToken ct);
    Task PublishAsync(Guid matchmakingId, string eventName, string json, CancellationToken ct);
}

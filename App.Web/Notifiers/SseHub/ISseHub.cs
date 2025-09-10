using Microsoft.AspNetCore.Http;

namespace App.Web.Notifiers.SseHub;

public interface ISseHub
{
    void Subscribe(Guid matchmakingId, HttpResponse response, CancellationToken ct);
    Task PublishAsync(Guid matchmakingId, string eventName, string json, CancellationToken ct);
}

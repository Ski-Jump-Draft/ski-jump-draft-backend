using App.Web.Sse;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controller;

[ApiController]
[Route("matchmaking/stream")]
public class MatchmakingStreamController(ISseStream stream) : ControllerBase
{
    [HttpGet]
    public async Task Get(Guid matchmakingId, CancellationToken ct)
    {
        Response.Headers["Content-Type"] = "text/event-stream";

        var reader = stream.Subscribe(matchmakingId.ToString());
        try
        {
            await foreach (var msg in reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync(msg, ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        finally
        {
            stream.Unsubscribe(matchmakingId.ToString(), reader); 
        }
    }
}
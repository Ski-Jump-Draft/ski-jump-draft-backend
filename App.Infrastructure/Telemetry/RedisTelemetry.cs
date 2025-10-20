using System.Text.Json;
using App.Application.Telemetry;
using StackExchange.Redis;

namespace App.Infrastructure.Telemetry;

public class RedisTelemetry(IConnectionMultiplexer redis) : ITelemetry
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static string Key => "telemetry:events";

    public async Task Record(GameTelemetryEvent @event)
    {
        var json = JsonSerializer.Serialize(@event);
        await _db.ListRightPushAsync(Key, json);
    }
}
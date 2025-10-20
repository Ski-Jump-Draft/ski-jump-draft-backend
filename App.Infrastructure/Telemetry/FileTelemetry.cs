using System.Text.Json;
using App.Application.Telemetry;

namespace App.Infrastructure.Telemetry;

public class FileTelemetry(string filePath = "telemetry.ndjson") : ITelemetry
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task Record(GameTelemetryEvent @event)
    {
        var json = JsonSerializer.Serialize(@event);
        await _lock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, json + "\n");
        }
        finally
        {
            _lock.Release();
        }
    }
}
using System.Collections.Concurrent;
using App.Application.Telemetry;

namespace App.Infrastructure.Telemetry;

public class InMemoryTelemetry : ITelemetry
{
    private readonly ConcurrentQueue<GameTelemetryEvent> _events = new();

    public Task Record(GameTelemetryEvent @event)
    {
        _events.Enqueue(@event);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<GameTelemetryEvent> GetAll() => _events.ToArray();

    public void Clear() => _events.Clear();
}
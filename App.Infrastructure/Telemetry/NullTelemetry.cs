using App.Application.Telemetry;

namespace App.Infrastructure.Telemetry;

public class NullTelemetry : ITelemetry
{
    public Task Record(GameTelemetryEvent @event) => Task.CompletedTask;
}
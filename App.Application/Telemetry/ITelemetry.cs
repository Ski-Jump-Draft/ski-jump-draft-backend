namespace App.Application.Telemetry;

public interface ITelemetry
{
    Task Record(GameTelemetryEvent @event);
}

public record GameTelemetryEvent(
    string EventType, // e.g. "JumperPicked"
    Guid? GameId,
    Guid? MatchmakingId,
    Guid? SessionId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> Data // e.g. { durationMs: 1200, botPickAlgorithmRank: 4 }
);
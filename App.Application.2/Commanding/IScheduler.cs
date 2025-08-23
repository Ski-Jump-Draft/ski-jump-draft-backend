namespace App.Application._2.Commanding;

public interface IScheduler
{
    Task ScheduleAsync(string jobType, string payloadJson, DateTimeOffset runAt, string? uniqueKey = null, CancellationToken ct = default);
}

public record EndMatchmakingPayload(Guid MatchmakingId);

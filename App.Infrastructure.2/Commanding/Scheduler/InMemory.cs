using System.Collections.Concurrent;
using App.Application._2.Commanding;
using App.Application._2.Utility;

namespace App.Infrastructure._2.Commanding.Scheduler;

public class InMemory(ICommandBus commandBus, IJson json) : IScheduler
{
    private readonly ConcurrentDictionary<string, Task> _jobs = new();

    public async Task ScheduleAsync(
        string jobType,
        string payloadJson,
        DateTimeOffset runAt,
        string? uniqueKey = null,
        CancellationToken ct = default)
    {
        // jeśli chcesz unikalność
        if (uniqueKey != null && _jobs.ContainsKey(uniqueKey))
            return;

        var delay = runAt - DateTimeOffset.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        var job = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, ct);
                Console.WriteLine($"[Job:{jobType}] payload={payloadJson}");
                

                switch (jobType)
                {
                    case "EndMatchmaking":
                        var payload = json.Deserialize<EndMatchmakingPayload>(payloadJson);
                        var command =
                            new Application._2.UseCase.Matchmaking.EndMatchmaking.Command(payload.MatchmakingId);
                        await commandBus.SendAsync<Application._2.UseCase.Matchmaking.EndMatchmaking.Command, Application._2.UseCase.Matchmaking.EndMatchmaking.Result>(command, ct);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            catch (TaskCanceledException) { }
        }, ct);

        if (uniqueKey != null)
            _jobs[uniqueKey] = job;

        await Task.CompletedTask;
    }
}

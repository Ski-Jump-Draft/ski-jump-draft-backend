using System.Collections.Concurrent;
using App.Application._2.Commanding;
using App.Application._2.Utility;

namespace App.Infrastructure._2.Commanding.Scheduler;

public class InMemory(ICommandBus commandBus, IJson json, IMyLogger myLogger) : IScheduler
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
                        await HandleEndMatchmaking(payloadJson, ct);
                        break;
                    case "StartGame":
                        await HandleStartGame(payloadJson, ct);
                        break;
                    case "StartPreDraft":
                        await HandleStartPreDraft(payloadJson, ct);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            catch (TaskCanceledException) 
            { 
                // OK, to można zignorować
            }
            catch (Exception ex)
            {
                myLogger.Error($"Scheduler job {jobType} failed: {ex.Message}");
                myLogger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }, ct);

        if (uniqueKey != null)
            _jobs[uniqueKey] = job;

        await Task.CompletedTask;
    }



    private async Task HandleEndMatchmaking(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<EndMatchmakingPayload>(payloadJson);
        var command =
            new Application._2.UseCase.Matchmaking.EndMatchmaking.Command(payload.MatchmakingId);
        await commandBus.SendAsync<Application._2.UseCase.Matchmaking.EndMatchmaking.Command, Application._2.UseCase.Matchmaking.EndMatchmaking.Result>(command, ct);
    }
    private async Task HandleStartGame(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartGamePayload>(payloadJson);
        var command = new Application._2.UseCase.Game.StartGame.Command(payload.MatchmakingId);
        myLogger.Debug("Sending StartGame command to a command bus...");
        await commandBus.SendAsync<Application._2.UseCase.Game.StartGame.Command, Application._2.UseCase.Game.StartGame.Result>(command, ct);
    }
    private async Task HandleStartPreDraft(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartPreDraftPayload>(payloadJson);
        var command = new Application._2.UseCase.Game.StartPreDraft.Command(payload.GameId);
        await commandBus.SendAsync<Application._2.UseCase.Game.StartPreDraft.Command, Application._2.UseCase.Game.StartPreDraft.Result>(command, ct);
    }
}

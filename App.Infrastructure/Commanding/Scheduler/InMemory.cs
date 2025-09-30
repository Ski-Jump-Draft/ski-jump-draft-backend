using System.Collections.Concurrent;
using App.Application.Commanding;
using App.Application.Utility;

namespace App.Infrastructure.Commanding.Scheduler;

public class InMemory(ICommandBus commandBus, IJson json, IMyLogger logger) : IScheduler
{
    private readonly ConcurrentDictionary<string, Task> _jobs = new();

    public async Task ScheduleAsync(
        string jobType,
        string payloadJson,
        DateTimeOffset runAt,
        string? uniqueKey = null,
        CancellationToken ct = default)
    {
        logger.Debug("Scheduling job: " + jobType + "");
        // jeśli chcesz unikalność
        if (uniqueKey != null && _jobs.ContainsKey(uniqueKey))
        {
            logger.Debug($"Job key is not unique ({uniqueKey})");
            return;
        }

        var delay = runAt - DateTimeOffset.UtcNow;
        logger.Debug($"[Job:{jobType}]: delay {delay}.");
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        var job = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay);
                logger.Info($"[Job:{jobType}] payload={payloadJson}");

                switch (jobType)
                {
                    case "EndMatchmaking":
                        await HandleEndMatchmaking(payloadJson, CancellationToken.None);
                        break;
                    case "StartGame":
                        await HandleStartGame(payloadJson, CancellationToken.None);
                        break;
                    case "StartPreDraft":
                        await HandleStartPreDraft(payloadJson, CancellationToken.None);
                        break;
                    case "SimulateJumpInGame":
                        await HandleSimulateJumpInGame(payloadJson, CancellationToken.None);
                        break;
                    case "StartNextPreDraftCompetition":
                        await HandleStartNextPreDraftCompetition(payloadJson, CancellationToken.None);
                        break;
                    case "StartDraft":
                        await HandleStartDraft(payloadJson, CancellationToken.None);
                        break;
                    case "StartMainCompetition":
                        await HandleStartMainCompetition(payloadJson, CancellationToken.None);
                        break;
                    case "PickJumper":
                        await HandlePickJumper(payloadJson, CancellationToken.None);
                        break;
                    case "PassPick":
                        await HandlePassPick(payloadJson, CancellationToken.None);
                        break;
                    case "PickByBot":
                        await HandlePickByBot(payloadJson, CancellationToken.None);
                        break;
                    case "EndGame":
                        await HandleEndGame(payloadJson, CancellationToken.None);
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported job type: " + jobType);
                }
            }
            catch (TaskCanceledException)
            {
                // OK, to można zignorować
            }
            catch (Application.UseCase.Game.PassPick.PassPickingLockedException)
            {
            }
            catch (Exception ex)
            {
                logger.Error($"Scheduler job {jobType} failed: {ex.Message}");
                logger.Error($"Stack trace: {ex.StackTrace}");
                throw;
            }
        });

        if (uniqueKey != null)
            _jobs[uniqueKey] = job;

        await Task.CompletedTask;
    }


    private async Task HandleEndMatchmaking(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<EndMatchmakingPayload>(payloadJson);
        var command =
            new Application.UseCase.Matchmaking.EndMatchmaking.Command(payload.MatchmakingId);
        await commandBus
            .SendAsync<Application.UseCase.Matchmaking.EndMatchmaking.Command,
                Application.UseCase.Matchmaking.EndMatchmaking.Result>(command, ct);
    }

    private async Task HandleStartGame(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartGamePayload>(payloadJson);
        var command = new Application.UseCase.Game.StartGame.Command(payload.MatchmakingId);
        logger.Debug("Sending StartGame command to a command bus...");
        await commandBus
            .SendAsync<Application.UseCase.Game.StartGame.Command, Application.UseCase.Game.StartGame.Result>(
                command, ct);
    }

    private async Task HandleStartPreDraft(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartPreDraftPayload>(payloadJson);
        var command = new Application.UseCase.Game.StartPreDraft.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.StartPreDraft.Command,
                Application.UseCase.Game.StartPreDraft.Result>(command, ct);
    }

    private async Task HandleSimulateJumpInGame(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartPreDraftPayload>(payloadJson);
        var command = new Application.UseCase.Game.SimulateJump.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.SimulateJump.Command,
                Application.UseCase.Game.SimulateJump.Result>(command, ct);
    }

    private async Task HandleStartNextPreDraftCompetition(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartNextPreDraftCompetitionPayload>(payloadJson);
        var command = new Application.UseCase.Game.StartNextPreDraftCompetition.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.StartNextPreDraftCompetition.Command,
                Application.UseCase.Game.StartNextPreDraftCompetition.Result>(command, ct);
    }

    private async Task HandleStartDraft(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartDraftPayload>(payloadJson);
        var command = new Application.UseCase.Game.StartDraft.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.StartDraft.Command,
                Application.UseCase.Game.StartDraft.Result>(command, ct);
    }

    private async Task HandleStartMainCompetition(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<StartMainCompetitionPayload>(payloadJson);
        var command = new Application.UseCase.Game.StartMainCompetition.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.StartMainCompetition.Command,
                Application.UseCase.Game.StartMainCompetition.Result>(command, ct);
    }

    private async Task HandlePickJumper(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<PickJumperPayload>(payloadJson);
        var command =
            new Application.UseCase.Game.PickJumper.Command(payload.GameId, payload.PlayerId, payload.JumperId);
        await commandBus
            .SendAsync<Application.UseCase.Game.PickJumper.Command,
                Application.UseCase.Game.PickJumper.Result>(command, CancellationToken.None);
    }

    private async Task HandlePassPick(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<PassPickPayload>(payloadJson);
        var command = new Application.UseCase.Game.PassPick.Command(payload.GameId, payload.PlayerId, payload.TurnIndex);
        await commandBus
            .SendAsync<Application.UseCase.Game.PassPick.Command,
                Application.UseCase.Game.PassPick.Result>(command, ct);
    }

    private async Task HandlePickByBot(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<PickByBot>(payloadJson);
        var command = new Application.UseCase.Game.PickByBot.Command(payload.GameId, payload.PlayerId);
        await commandBus
            .SendAsync<Application.UseCase.Game.PickByBot.Command,
                Application.UseCase.Game.PickByBot.Result>(command, ct);
    }

    private async Task HandleEndGame(string payloadJson, CancellationToken ct)
    {
        var payload = json.Deserialize<EndGamePayload>(payloadJson);
        var command = new Application.UseCase.Game.EndGame.Command(payload.GameId);
        await commandBus
            .SendAsync<Application.UseCase.Game.EndGame.Command,
                Application.UseCase.Game.EndGame.Result>(command, ct);
    }
}
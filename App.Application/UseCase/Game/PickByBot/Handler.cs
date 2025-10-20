using System.Collections.ObjectModel;
using App.Application.Acl;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Policy.DraftBotPickTime;
using App.Application.Policy.DraftPicker;
using App.Application.Service;
using App.Application.Telemetry;
using App.Application.Utility;
using App.Domain.Game;

namespace App.Application.UseCase.Game.PickByBot;

public record Command(
    Guid GameId,
    Guid PlayerId
) : ICommand, ICommand<Result>;

public record Result(Guid JumperId);

public class Handler(
    IGames games,
    IDraftBotPickTime draftBotPickTime,
    IScheduler scheduler,
    IJson json,
    IDraftPicker draftPicker,
    IClock clock,
    ITelemetry telemetry,
    IGameJumperAcl gameJumperAcl) : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        var timeoutPolicy = game.Settings.DraftSettings.TimeoutPolicy;
        var timeoutSeconds = timeoutPolicy.ToSeconds();
        var pickTime = draftBotPickTime.Get(timeoutPolicy) - TimeSpan.FromMilliseconds(100);
        if (pickTime.TotalSeconds >= timeoutSeconds)
        {
            throw new DraftBotPickTooLongException(pickTime.TotalSeconds);
        }

        var pickedGameJumperId = await draftPicker.Pick(game, ct);

        int? pickedGameJumperRankInAlgorithm = null;
        if (draftPicker is IDraftPickerWithJumpersRanking draftPickerWithJumpersRanking)
        {
            pickedGameJumperRankInAlgorithm =
                draftPickerWithJumpersRanking.JumperRank(pickedGameJumperId);
        }

        var gameWorldJumperId = gameJumperAcl.GetGameWorldJumper(pickedGameJumperId).GameWorldJumperId;

        var now = clock.Now();

        _ = Task.Run(async () => { await Task.Delay(pickTime, ct); }, ct);

        await scheduler.ScheduleAsync("PickJumper",
            json.Serialize(new { command.GameId, command.PlayerId, JumperId = pickedGameJumperId, IsBot = true }),
            now.Add(pickTime),
            $"PickJumper:{command.GameId}_{pickedGameJumperId}", ct);

        await RecordTelemetry(command, gameWorldJumperId, pickedGameJumperId, game.DraftCurrentPickIndex.Value, pickTime,
            timeoutSeconds,
            pickedGameJumperRankInAlgorithm);

        return new Result(pickedGameJumperId);
    }

    private async Task RecordTelemetry(Command command, Guid gameWorldJumperId, Guid pickedGameJumperId, int pickIndex,
        TimeSpan pickTime,
        int? timeoutSeconds, int? pickedGameJumperRankInAlgorithm)
    {
        var data = new Dictionary<string, object>()
        {
            ["GameWorldJumperId"] = gameWorldJumperId,
            ["GameJumperId"] = pickedGameJumperId,
            ["PickTimeSeconds"] = pickTime.TotalSeconds,
            ["PickIndex"] = pickIndex
        };
        if (timeoutSeconds is not null)
            data["TimeoutSeconds"] = timeoutSeconds;
        if (pickedGameJumperRankInAlgorithm is not null)
            data["RankInBotPickAlgorithm"] = pickedGameJumperRankInAlgorithm;

        await telemetry.Record(new GameTelemetryEvent("SchedulePickByBot", command.GameId, null, null, clock.Now(),
            data));
    }
}

public class DraftBotPickTooLongException(double TotalSeconds, string? message = null) : Exception(message);
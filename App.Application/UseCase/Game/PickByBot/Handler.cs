using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Policy.DraftBotPickTime;
using App.Application.Policy.DraftPicker;
using App.Application.Service;
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
    IBotPassPickLock botPassPickLock) : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        botPassPickLock.Lock(command.GameId, command.PlayerId);
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

        var now = clock.Now();
        await scheduler.ScheduleAsync("PickJumper",
            json.Serialize(new { command.GameId, command.PlayerId, JumperId = pickedGameJumperId }), now.Add(pickTime),
            $"PickJumper:{command.GameId}_{pickedGameJumperId}", ct);

        await Task.Delay(pickTime, ct);

        return new Result(pickedGameJumperId);
    }
}

public class DraftBotPickTooLongException(double TotalSeconds, string? message = null) : Exception(message);
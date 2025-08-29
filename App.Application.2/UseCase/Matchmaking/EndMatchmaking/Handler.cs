using App.Application._2.Commanding;
using App.Application._2.Exceptions;
using App.Application._2.Extensions;
using App.Application._2.Matchmaking;
using App.Application._2.Messaging;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Utility;
using App.Domain._2.Matchmaking;

namespace App.Application._2.UseCase.Matchmaking.EndMatchmaking;

public record Command(
    Guid MatchmakingId
) : ICommand<Result>;

public record Result(bool HasSucceeded);

public class Handler(
    IJson json,
    IMatchmakings matchmakings,
    IMatchmakingNotifier notifier,
    IMatchmakingSchedule matchmakingSchedule,
    IScheduler scheduler,
    IClock clock, IMyLogger logger)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct).AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));;

        var (endedMatchmaking, hasSucceeded) = matchmaking.End().ResultValue;

        await matchmakings.Add(endedMatchmaking, ct);

        if (hasSucceeded)
        {
            const int seconds = 2;
            logger.Info($"Game Start is scheduled in {seconds} seconds. MatchmakingId: {command.MatchmakingId}");
            await scheduler.ScheduleAsync(
                jobType: "StartGame",
                payloadJson: json.Serialize(new { MatchmakingId = command.MatchmakingId }),
                runAt: clock.Now().AddSeconds(seconds),
                uniqueKey: $"StartGame:{command.MatchmakingId}",
                ct: ct
            );
            matchmakingSchedule.EndMatchmaking(command.MatchmakingId);
        }

        await notifier.MatchmakingUpdated(MatchmakingUpdatedDtoMapper.FromDomain(endedMatchmaking));

        return new Result(hasSucceeded);
    }
}
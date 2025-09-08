using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging;
using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Matchmaking.EndMatchmaking;

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
        
        logger.Info($"Tried to end matchmaking. MatchmakingId: {command.MatchmakingId}. hasSucceeded: {hasSucceeded}. Status: {endedMatchmaking.Status_}. Players count: {endedMatchmaking.PlayersCount}.");

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

        await notifier.MatchmakingUpdated(MatchmakingNotifierMappers.MatchmakingUpdatedFromDomain(endedMatchmaking));

        return new Result(hasSucceeded);
    }
}
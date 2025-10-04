using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Matchmaking;

namespace App.Application.UseCase.Matchmaking.TryEndMatchmaking;

public record Command(
    Guid MatchmakingId
) : ICommand<Result>;

public record Result(bool HasEnded);

public class Handler(
    IJson json,
    IMatchmakings matchmakings,
    IMatchmakingNotifier matchmakingNotifier,
    IScheduler scheduler,
    IClock clock,
    IMyLogger logger,
    IBotRegistry botRegistry,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));

        var matchmakingBots = botRegistry.MatchmakingBots(command.MatchmakingId);
        var onlyBotsExist = matchmakingBots.Count == matchmaking.PlayersCount;
        var now = clock.Now();

        if (matchmaking.ShouldEnd(now))
        {
            if (onlyBotsExist)
            {
                logger.Info($"Did not start a matchmaking cause only bots are present. MatchmakingId: {
                    command.MatchmakingId
                }, bots count: {matchmakingBots.Count}.");
                var failedMatchmaking = matchmaking.Fail("Can not end a matchmaking only with bots", now).ResultValue;
                await PersistMatchmaking(failedMatchmaking, ct);
                return new Result(false);
            }

            var (endedMatchmaking, hasSucceeded) = matchmaking.End(now).ResultValue;

            logger.Info($"Tried to end matchmaking. MatchmakingId: {command.MatchmakingId}. hasSucceeded: {hasSucceeded
            }. Status: {endedMatchmaking.Status_}. Players count: {endedMatchmaking.PlayersCount}.");

            await PersistMatchmaking(endedMatchmaking, ct);

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
            }

            await matchmakingNotifier.MatchmakingUpdated(matchmakingUpdatedDtoMapper.FromDomain(endedMatchmaking));

            return new Result(true);
        }

        logger.Debug($"Matchmaking {command.MatchmakingId} is not ready to end.");

        await scheduler.ScheduleAsync(
            jobType: "TryEndMatchmaking",
            payloadJson: json.Serialize(new { MatchmakingId = command.MatchmakingId }),
            runAt: now.Add(TimeSpan.FromMilliseconds(1000)),
            uniqueKey: $"TryEndMatchmaking:{command.MatchmakingId}_{now.ToString()}",
            ct: ct
        );
        return new Result(false);
    }

    private async Task PersistMatchmaking(Domain.Matchmaking.Matchmaking endedMatchmaking, CancellationToken ct)
    {
        await matchmakings.Add(endedMatchmaking, ct);
    }
}
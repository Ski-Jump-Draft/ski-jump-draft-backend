using System.Collections.Immutable;
using App.Application._2.Commanding;
using App.Application._2.Extensions;
using App.Application._2.Matchmaking;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Utility;
using App.Domain._2.Matchmaking;

namespace App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking;

public record Command(
    string Nick
) : ICommand<Result>;

public record Result(Guid MatchmakingId, string Nick, Guid PlayerId);

public class Handler(
    IGuid guid,
    IMatchmakings matchmakings,
    ILogger logger,
    Domain._2.Matchmaking.Settings globalMatchmakingSettings,
    IScheduler scheduler,
    IJson json,
    IClock clock,
    IMatchmakingSchedule matchmakingSchedule,
    IMatchmakingNotifier matchmakingNotifier)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        logger.Info($"{command.Nick} requested the join to a matchmaking ");
        var nickOption = PlayerModule.NickModule.create(command.Nick);
        if (nickOption.IsNone())
        {
            throw new Exception("Nick is invalid");
        }

        var nick = nickOption.Value;
        var (matchmaking, justCreated) = await FindOrCreateMatchmakingAsync(ct);
        var player = new Domain._2.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick);
        var joinResult = matchmaking.Join(player);
        var (matchmakingAfterJoin, correctedNick) = joinResult.ResultValue;
        await matchmakings.Add(matchmakingAfterJoin, ct);

        var matchmakingDuration = TimeSpan.FromMinutes(2);

        if (justCreated)
        {
            await scheduler.ScheduleAsync(jobType: "EndMatchmaking",
                json.Serialize(new { MatchmakingId = matchmaking.Id_.Item }), clock.Now().Add(matchmakingDuration),
                $"EndMatchmaking:{matchmaking.Id_.Item}", ct);
            matchmakingSchedule.StartMatchmaking(matchmaking.Id_.Item, matchmakingDuration);
        }

        await matchmakingNotifier.MatchmakingUpdated(MatchmakingDtoMapper.FromDomain(matchmakingAfterJoin));

        logger.Info($"{player.Nick} joined the matchmaking ({matchmaking.Id_.Item})");

        return new Result(matchmaking.Id_.Item, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private async Task<MatchmakingDto> FindOrCreateMatchmakingAsync(CancellationToken ct)
    {
        var matchmmakingsInProgress = (await matchmakings.GetInProgress(ct)).ToImmutableArray();
        switch (matchmmakingsInProgress.Length)
        {
            case > 1:
                throw new NotImplementedException(
                    "We are not supporting multiple matchmaking at the moment. Please report this bug.");
            case 1:
                return new MatchmakingDto(matchmmakingsInProgress.Single(), JustCreated: false);
            default:
            {
                var newMatchmaking =
                    Domain._2.Matchmaking.Matchmaking.Create(MatchmakingId.NewMatchmakingId(guid.NewGuid()),
                        globalMatchmakingSettings);
                return new MatchmakingDto(newMatchmaking, JustCreated: true);
            }
        }
    }

    private record MatchmakingDto(Domain._2.Matchmaking.Matchmaking Matchmaking, bool JustCreated);
}
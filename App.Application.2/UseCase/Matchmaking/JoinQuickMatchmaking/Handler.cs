using System.Collections.Immutable;
using App.Application._2.Commanding;
using App.Application._2.Extensions;
using App.Application._2.Matchmaking;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Utility;
using App.Domain._2.Game;
using App.Domain._2.GameWorld;
using App.Domain._2.Matchmaking;
using PlayerId = App.Domain._2.Matchmaking.PlayerId;
using PlayerModule = App.Domain._2.Matchmaking.PlayerModule;

namespace App.Application._2.UseCase.Matchmaking.JoinQuickMatchmaking;

public record Command(
    string Nick
) : ICommand<Result>;

public record Result(Guid MatchmakingId, string Nick, Guid PlayerId);

public class Handler(
    IGuid guid,
    IMatchmakings matchmakings,
    IMyLogger myLogger,
    Domain._2.Matchmaking.Settings globalMatchmakingSettings,
    IScheduler scheduler,
    IJson json,
    IClock clock,
    IMatchmakingSchedule matchmakingSchedule,
    IMatchmakingNotifier matchmakingNotifier,
    IJumpers jumpers,
    IMatchmakingDurationCalculator matchmakingDurationCalculator,
    IGames games)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var nickOption = PlayerModule.NickModule.create(command.Nick);
        if (nickOption.IsNone())
        {
            throw new Exception("Nick is invalid");
        }

        var nick = nickOption.Value;
        var (matchmaking, justCreated) = await FindOrCreateMatchmakingAsync(ct);
        var player = new Domain._2.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick);
        var joinResult = matchmaking.Join(player);
        if (joinResult.IsError)
        {
            if (joinResult.ErrorValue.IsTooManyPlayers)
            {
                throw new RoomIsFullException();
            }

            if (joinResult.ErrorValue.IsAlreadyJoined)
            {
                throw new PlayerAlreadyJoinedException();
            }

            throw new Exception($"Unknown error: {joinResult.ErrorValue}");
        }

        var (matchmakingAfterJoin, correctedNick) = joinResult.ResultValue;
        await matchmakings.Add(matchmakingAfterJoin, ct);

        var matchmakingDuration = matchmakingDurationCalculator.Calculate(matchmakingAfterJoin);

        if (justCreated)
        {
            await scheduler.ScheduleAsync(jobType: "EndMatchmaking",
                json.Serialize(new { MatchmakingId = matchmaking.Id_.Item }), clock.Now().Add(matchmakingDuration),
                $"EndMatchmaking:{matchmaking.Id_.Item}", ct);
            matchmakingSchedule.StartMatchmaking(matchmaking.Id_.Item, matchmakingDuration);
        }

        await matchmakingNotifier.MatchmakingUpdated(MatchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterJoin));

        myLogger.Info($"{player.Nick} joined the matchmaking ({matchmaking.Id_.Item})");

        return new Result(matchmaking.Id_.Item, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private async Task<MatchmakingDto> FindOrCreateMatchmakingAsync(CancellationToken ct)
    {
        var matchmmakingsInProgress = (await matchmakings.GetInProgress(ct)).ToImmutableArray();
        var gamesInProgress = await games.GetInProgressCount(ct);
        switch (matchmmakingsInProgress.Length, gamesInProgress)
        {
            case (_, >= 1):
                throw new MultipleGamesNotSupportedException();
            case (1, 0):
                return new MatchmakingDto(matchmmakingsInProgress.Single(), JustCreated: false);
            // 0 matchmakings, 0 games
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

public class MultipleGamesNotSupportedException(string? message = null) : Exception(message);

public class RoomIsFullException(string? message = null) : Exception(message);

public class PlayerAlreadyJoinedException(string? message = null) : Exception(message);
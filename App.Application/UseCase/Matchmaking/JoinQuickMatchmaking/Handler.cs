using System.Collections.Immutable;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using App.Domain.Matchmaking;
using PlayerId = App.Domain.Matchmaking.PlayerId;
using PlayerModule = App.Domain.Matchmaking.PlayerModule;

namespace App.Application.UseCase.Matchmaking.JoinQuickMatchmaking;

public record Command(
    string Nick
) : ICommand<Result>;

public record Result(Guid MatchmakingId, string Nick, Guid PlayerId);

public class Handler(
    IGuid guid,
    IMatchmakings matchmakings,
    IMyLogger myLogger,
    Domain.Matchmaking.Settings globalMatchmakingSettings,
    IScheduler scheduler,
    IJson json,
    IClock clock,
    IMatchmakingSchedule matchmakingSchedule,
    IMatchmakingNotifier matchmakingNotifier,
    IMatchmakingDurationCalculator matchmakingDurationCalculator,
    IGames games,
    IBotRegistry botRegistry)
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

        return await JoinPlayerToMatchmaking(nick, matchmaking, justCreated, ct);
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
                    Domain.Matchmaking.Matchmaking.Create(MatchmakingId.NewMatchmakingId(guid.NewGuid()),
                        globalMatchmakingSettings);
                return new MatchmakingDto(newMatchmaking, JustCreated: true);
            }
        }
    }

    private async Task<Result> JoinPlayerToMatchmaking(PlayerModule.Nick nick,
        Domain.Matchmaking.Matchmaking matchmaking, bool justCreated, CancellationToken ct)
    {
        var player = new Domain.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick);
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

        botRegistry.RegisterMatchmakingBot(matchmaking.Id_.Item, player.Id.Item);
        await matchmakingNotifier.PlayerJoined(
            MatchmakingNotifierMappers.PlayerJoinedFromDomain(player.Id.Item,
                PlayerModule.NickModule.value(correctedNick), matchmakingAfterJoin));
        await matchmakingNotifier.MatchmakingUpdated(
            MatchmakingNotifierMappers.MatchmakingUpdatedFromDomain(matchmakingAfterJoin));


        myLogger.Info($"{correctedNick} joined the matchmaking ({matchmaking.Id_.Item})");

        return new Result(matchmaking.Id_.Item, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private record MatchmakingDto(Domain.Matchmaking.Matchmaking Matchmaking, bool JustCreated);
}

public class MultipleGamesNotSupportedException(string? message = null) : Exception(message);

public class RoomIsFullException(string? message = null) : Exception(message);

public class PlayerAlreadyJoinedException(string? message = null) : Exception(message);
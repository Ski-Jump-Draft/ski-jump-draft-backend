using System.Collections.Immutable;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using App.Domain.Matchmaking;
using PlayerId = App.Domain.Matchmaking.PlayerId;
using PlayerModule = App.Domain.Matchmaking.PlayerModule;

namespace App.Application.UseCase.Matchmaking.JoinQuickMatchmaking;

public record Command(
    string Nick,
    bool IsBot
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
    IMatchmakingNotifier matchmakingNotifier,
    IGames games,
    IBotRegistry botRegistry,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper
)
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

        return await JoinPlayerToMatchmaking(nick, matchmaking, justCreated, command.IsBot, ct);
    }

    private async Task<MatchmakingDto> FindOrCreateMatchmakingAsync(CancellationToken ct)
    {
        var matchmmakingsInProgress = (await matchmakings.GetInProgress(ct)).ToImmutableArray();
        var gamesInProgress = await games.GetInProgressCount(ct);
        var now = clock.Now();
        switch (matchmmakingsInProgress.Length, gamesInProgress)
        {
            case (_, >= 1):
                throw new MultipleGamesNotSupportedException();
            case (1, 0):
                var matchmaking = matchmmakingsInProgress.Single();
                return new MatchmakingDto(matchmmakingsInProgress.Single(), JustCreated: false);
            // 0 matchmakings, 0 games
            default:
            {
                var newMatchmaking =
                    Domain.Matchmaking.Matchmaking.CreateNew(MatchmakingId.NewMatchmakingId(guid.NewGuid()),
                        globalMatchmakingSettings, now);
                return new MatchmakingDto(newMatchmaking, JustCreated: true);
            }
        }
    }

    private async Task<Result> JoinPlayerToMatchmaking(PlayerModule.Nick nick,
        Domain.Matchmaking.Matchmaking matchmaking, bool justCreated, bool isBot, CancellationToken ct)
    {
        var player = new Domain.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick);
        var now = clock.Now();
        var joinResult = matchmaking.Join(player, now);
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

        if (justCreated)
        {
            now = clock.Now();
            await scheduler.ScheduleAsync(jobType: "TryEndMatchmaking",
                json.Serialize(new { MatchmakingId = matchmaking.Id_.Item }),
                now.Add(TimeSpan.FromMilliseconds(1000)),
                $"TryEndMatchmaking:{matchmaking.Id_.Item}_{now.ToString()}", ct);
        }

        if (isBot)
        {
            botRegistry.RegisterMatchmakingBot(matchmaking.Id_.Item, player.Id.Item);
        }

        await matchmakingNotifier.MatchmakingUpdated(matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterJoin));
        await matchmakingNotifier.PlayerJoined(matchmakingUpdatedDtoMapper.PlayerJoinedFromDomain(player.Id.Item,
            PlayerModule.NickModule.value(player.Nick), matchmakingAfterJoin));

        myLogger.Info($"{correctedNick} joined the matchmaking ({matchmaking.Id_.Item})");

        return new Result(matchmaking.Id_.Item, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private record MatchmakingDto(Domain.Matchmaking.Matchmaking Matchmaking, bool JustCreated);
}

public class MultipleGamesNotSupportedException(string? message = null) : Exception(message);

public class RoomIsFullException(string? message = null) : Exception(message);

public class PlayerAlreadyJoinedException(string? message = null) : Exception(message);
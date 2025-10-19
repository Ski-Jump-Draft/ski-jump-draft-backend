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
    IMyLogger logger,
    Domain.Matchmaking.Settings globalMatchmakingSettings,
    IScheduler scheduler,
    IJson json,
    IClock clock,
    IMatchmakingNotifier matchmakingNotifier,
    IGames games,
    IBotRegistry botRegistry,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper,
    IMatchmakingUpdatedDtoStorage matchmakingUpdatedDtoStorage,
    IPremiumMatchmakingGames matchmakingGames)
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
        var matchmmakingsInProgress = (await matchmakings.GetInProgress(MatchmakingType.Normal, ct)).ToImmutableArray();
        var allGamesInProgressCount = await games.GetInProgressCount(ct);
        var premiumMatchmakingGamesCount = await matchmakingGames.GetGamesCount();
        logger.Info("Premium matchmaking games: " + premiumMatchmakingGamesCount + "");
        var normalGamesInProgressCount = allGamesInProgressCount - premiumMatchmakingGamesCount;

        var now = clock.Now();
        var matchmakingsCount = matchmmakingsInProgress.Length;

        switch (matchmakingsCount, normalGamesInProgressCount)
        {
            case (> 1, _):
            case (_, >= 1):
                throw new MultipleGamesNotSupportedException();
            case (0, 0):
                var newMatchmaking =
                    Domain.Matchmaking.Matchmaking.CreateNew(MatchmakingId.NewMatchmakingId(guid.NewGuid()),
                        premium: false,
                        globalMatchmakingSettings, now);
                return new MatchmakingDto(newMatchmaking, JustCreated: true);
            case (1, 0):
                return new MatchmakingDto(matchmmakingsInProgress.Single(), JustCreated: false);
            default:
                throw new Exception($"Unknown error. Matchmaking count: {matchmakingsCount}, games in progress: {
                    allGamesInProgressCount}");
        }
    }

    private async Task<Result> JoinPlayerToMatchmaking(PlayerModule.Nick nick,
        Domain.Matchmaking.Matchmaking matchmaking, bool justCreated, bool isBot, CancellationToken ct)
    {
        var matchmakingGuid = matchmaking.Id_.Item;
        var now = clock.Now();
        var player = new Domain.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick, now);
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

        var matchmakingUpdatedDto = matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterJoin, now);
        logger.Info("Just created matchmaking? (id= " + matchmakingGuid + "): " + justCreated + "");
        if (justCreated)
        {
            now = clock.Now();
            await scheduler.ScheduleAsync(jobType: "TryEndMatchmaking",
                json.Serialize(new { MatchmakingId = matchmakingGuid }),
                now.Add(TimeSpan.FromMilliseconds(1000)),
                $"TryEndMatchmaking:{matchmakingGuid}_{now.ToString()}", ct);
        }

        await matchmakingUpdatedDtoStorage.Set(matchmakingGuid, matchmakingUpdatedDto);

        logger.Info("Is bot? (id= " + matchmakingGuid + "): " + isBot + "");
        if (isBot)
        {
            logger.Info($"Player {PlayerModule.NickModule.value(player.Nick)}, is it bot? {isBot}");
            botRegistry.RegisterMatchmakingBot(matchmakingGuid, player.Id.Item);
        }

        now = clock.Now();

        await matchmakingNotifier.MatchmakingUpdated(matchmakingUpdatedDto);
        await matchmakingNotifier.PlayerJoined(
            matchmakingUpdatedDtoMapper.PlayerJoinedFromDomain(player, matchmakingAfterJoin));

        logger.Info($"{correctedNick} joined the matchmaking ({matchmakingGuid})");

        return new Result(matchmakingGuid, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private record MatchmakingDto(Domain.Matchmaking.Matchmaking Matchmaking, bool JustCreated);
}

public class MultipleGamesNotSupportedException(string? message = null) : Exception(message);

public class RoomIsFullException(string? message = null) : Exception(message);

public class PlayerAlreadyJoinedException(string? message = null) : Exception(message);
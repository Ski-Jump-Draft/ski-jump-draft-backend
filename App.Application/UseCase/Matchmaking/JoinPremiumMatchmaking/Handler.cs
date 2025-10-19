using System.Collections.Immutable;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Extensions;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Utility;
using App.Domain.Matchmaking;
using PlayerId = App.Domain.Matchmaking.PlayerId;
using PlayerModule = App.Domain.Matchmaking.PlayerModule;

namespace App.Application.UseCase.Matchmaking.JoinPremiumMatchmaking;

public record Command(
    string Nick,
    bool IsBot,
    string Password
) : ICommand<Result>;

public record Result(Guid MatchmakingId, string Nick, Guid PlayerId);

public class Handler(
    IGuid guid,
    IMatchmakings matchmakings,
    IMyLogger logger,
    Domain.Matchmaking.Settings globalMatchmakingSettings,
    IScheduler scheduler,
    IJson json,
    IMatchmakingNotifier matchmakingNotifier,
    IBotRegistry botRegistry,
    MatchmakingUpdatedDtoMapper matchmakingUpdatedDtoMapper,
    IMatchmakingUpdatedDtoStorage matchmakingUpdatedDtoStorage,
    IPremiumMatchmakingGames premiumMatchmakingGames,
    IPremiumMatchmakingConfigurationStorage premiumMatchmakingConfigurationStorage,
    IClock clock,
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

        var joinDateTime = clock.Now();
        var player = new Domain.Matchmaking.Player(PlayerId.NewPlayerId(guid.NewGuid()), nick, joinDateTime);

        var configs = await premiumMatchmakingConfigurationStorage.PremiumMatchmakingConfigs;
        logger.Info("Premium matchmaking configs: " + string.Join("  |  ", configs) + "");

        var passwordIsValid =
            await premiumMatchmakingConfigurationStorage.PremiumMatchmakingPasswordIsValid(command.Password);
        if (!passwordIsValid)
        {
            throw new InvalidPasswordException();
        }

        var gameAlreadyRunsByPremiumMatchmaking =
            await premiumMatchmakingGames.GameRunsByPremiumMatchmaking(command.Password);

        logger.Info($"Game already run by premium matchmaking? (password= {command.Password}): {
            gameAlreadyRunsByPremiumMatchmaking}");

        if (gameAlreadyRunsByPremiumMatchmaking)
        {
            throw new PrivateServerInUse();
        }

        var (matchmaking, justCreated) = await FindOrCreateMatchmakingAsync(command.Password, ct);
        // await matchmakings.Add(matchmaking, ct);
        var matchmakingId = matchmaking.Id_.Item;

        DateTimeOffset now;
        if (justCreated)
        {
            await premiumMatchmakingGames.Add(password: command.Password, matchmakingId: matchmakingId);
            now = clock.Now();
            await scheduler.ScheduleAsync(jobType: "TryEndMatchmaking",
                json.Serialize(new { MatchmakingId = matchmakingId }),
                now.Add(TimeSpan.FromMilliseconds(1000)),
                $"TryEndMatchmaking:{matchmakingId}_{now.ToString()}", ct);
        }

        var (matchmakingAfterJoin, correctedNick) =
            JoinPlayerToMatchmaking(player, joinDateTime, matchmaking);

        await matchmakings.Add(matchmakingAfterJoin, ct);

        now = clock.Now();
        var matchmakingUpdatedDto = matchmakingUpdatedDtoMapper.FromDomain(matchmakingAfterJoin, botRegistry, now);
        logger.Info("Just created matchmaking? (id= " + matchmakingId + "): " + justCreated + "");

        await matchmakingUpdatedDtoStorage.Set(matchmakingId, matchmakingUpdatedDto);

        logger.Info("Is bot? (id= " + matchmakingId + "): " + command.IsBot + "");

        if (command.IsBot)
        {
            logger.Info("Matchmaking players count after bot join: " + matchmakingAfterJoin.Players_.Count + "");
            botRegistry.RegisterMatchmakingBot(matchmakingId, player.Id.Item);
        }

        await matchmakingNotifier.MatchmakingUpdated(matchmakingUpdatedDto);
        await matchmakingNotifier.PlayerJoined(
            matchmakingUpdatedDtoMapper.PlayerJoinedFromDomain(player, command.IsBot, matchmakingAfterJoin));

        logger.Info($"{correctedNick} joined the matchmaking ({matchmakingId})");

        return new Result(matchmakingId, PlayerModule.NickModule.value(correctedNick), player.Id.Item);
    }

    private async Task<MatchmakingDto> FindOrCreateMatchmakingAsync(string password, CancellationToken ct)
    {
        var matchmakingId = await premiumMatchmakingGames.GetPremiumMatchmakingId(password);
        var matchmakingIsRunning = matchmakingId is not null;

        if (matchmakingIsRunning)
        {
            var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(matchmakingId!.Value), ct);
            return new MatchmakingDto(
                matchmaking.OrThrow($"Matchmaking not found (id= {matchmakingId})"),
                JustCreated: false);
        }

        var now = clock.Now();
        var newMatchmaking =
            Domain.Matchmaking.Matchmaking.CreateNew(MatchmakingId.NewMatchmakingId(guid.NewGuid()), premium: true,
                globalMatchmakingSettings, now);
        return new MatchmakingDto(newMatchmaking, JustCreated: true);
    }

    private static (Domain.Matchmaking.Matchmaking matchmaking, PlayerModule.Nick correctedNick)
        JoinPlayerToMatchmaking(Domain.Matchmaking.Player player, DateTimeOffset joinDateTime,
            Domain.Matchmaking.Matchmaking matchmaking)
    {
        var joinResult = matchmaking.Join(player, joinDateTime);

        if (!joinResult.IsError) return (joinResult.ResultValue.Item1, joinResult.ResultValue.Item2);

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

    private record MatchmakingDto(Domain.Matchmaking.Matchmaking Matchmaking, bool JustCreated);
}

public class InvalidPasswordException(string? message = null) : Exception(message);

public class PrivateServerInUse(string? message = null) : Exception(message);

public class RoomIsFullException(string? message = null) : Exception(message);

public class PlayerAlreadyJoinedException(string? message = null) : Exception(message);
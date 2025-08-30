using App.Application._2.Acl;
using App.Application._2.Commanding;
using App.Application._2.Exceptions;
using App.Application._2.Extensions;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Messaging.Notifiers.Mapper;
using App.Application._2.Policy.GameHillSelector;
using App.Application._2.Policy.GameJumpersSelector;
using App.Application._2.Utility;
using App.Domain._2.Game;
using App.Domain._2.GameWorld;
using App.Domain._2.Matchmaking;
using Microsoft.FSharp.Collections;
using HillModule = App.Domain._2.Competition.HillModule;
using PlayerId = App.Domain._2.Game.PlayerId;
using PlayerModule = App.Domain._2.Game.PlayerModule;

namespace App.Application._2.UseCase.Game.StartGame;

public record Command(
    Guid MatchmakingId
) : ICommand<Result>;

public record Result(Guid GameId);

public class Handler(
    IJson json,
    IMatchmakings matchmakings,
    IGames games,
    IGameNotifier gameNotifier,
    IGameJumpersSelector jumpersSelector,
    Domain._2.Game.Settings globalGameSettings,
    IScheduler scheduler,
    IClock clock,
    IGameHillSelector hillSelector,
    IGuid guid,
    IGameJumperAcl gameJumperAcl,
    ICompetitionJumperAcl competitionJumperAcl,
    IMyLogger logger,
    IHills hills)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.MatchmakingId));

        if (!matchmaking.HasSucceeded)
        {
            throw new Exception("Matchmaking has not succeeded");
        }

        logger.Info($"Starting game for a succeeded matchmaking {command.MatchmakingId}");

        var selectedJumperDtos = await jumpersSelector.Select(ct);

        var selectedHillGuid = await hillSelector.Select(ct);
        var gameWorldHill = await hills.GetById(Domain._2.GameWorld.HillId.NewHillId(selectedHillGuid), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(selectedHillGuid));

        var gameGuid = guid.NewGuid();
        var gameId = Domain._2.Game.GameId.NewGameId(gameGuid);

        var endedMatchmakingPlayers = matchmaking.Players_;
        var gamePlayersEnumerable = endedMatchmakingPlayers.Select(matchmakingPlayer =>
        {
            var gamePlayerGuid = guid.NewGuid();
            var gamePlayerId = PlayerId.NewPlayerId(gamePlayerGuid);
            var matchmakingPlayerNickString =
                Domain._2.Matchmaking.PlayerModule.NickModule.value(matchmakingPlayer.Nick)!;
            var gamePlayerNick = PlayerModule.NickModule.createWithSuffix(matchmakingPlayerNickString).Value;
            var gamePlayer = new Domain._2.Game.Player(gamePlayerId, gamePlayerNick);
            return gamePlayer;
        });
        var gamePlayers = Domain._2.Game.PlayersModule.create(ListModule.OfSeq(gamePlayersEnumerable)).ResultValue;

        var gameJumpersEnumerable = selectedJumperDtos.Select(selectedGameWorldJumperDto =>
        {
            var gameJumperId = guid.NewGuid();
            var gameJumperDto = new GameJumperDto(gameJumperId);
            var gameWorldJumperDto = new GameWorldJumperDto(selectedGameWorldJumperDto.Id);
            logger.Debug($"Mapping {gameJumperDto} with {gameWorldJumperDto}");
            gameJumperAcl.Map(gameWorldJumperDto, gameJumperDto);
            var competitionJumperId =
                guid.NewGuid(); // Od razu tworzymy competition jumpera, z którego korzystać będą inne Use Case'y
            competitionJumperAcl.Map(gameJumperDto, new CompetitionJumperDto(competitionJumperId));
            return new Domain._2.Game.Jumper(Domain._2.Game.JumperId.NewJumperId(gameJumperId));
        });
        var gameJumpers = Domain._2.Game.JumpersModule.create(ListModule.OfSeq(gameJumpersEnumerable));

        var competitionHillId = Domain._2.Competition.HillId.NewHillId(guid.NewGuid());
        var competitionHill = new Domain._2.Competition.Hill(competitionHillId,
            HillModule.KPointModule.tryCreate(Domain._2.GameWorld.HillModule.KPointModule.value(gameWorldHill.KPoint))
                .Value,
            HillModule.HsPointModule
                .tryCreate(Domain._2.GameWorld.HillModule.HsPointModule.value(gameWorldHill.HsPoint)).Value,
            HillModule.GatePointsModule
                .tryCreate(Domain._2.GameWorld.HillModule.GatePointsModule.value(gameWorldHill.GatePoints)).Value,
            HillModule.WindPointsModule
                .tryCreate(Domain._2.GameWorld.HillModule.WindPointsModule.value(gameWorldHill.HeadwindPoints)).Value,
            HillModule.WindPointsModule
                .tryCreate(Domain._2.GameWorld.HillModule.WindPointsModule.value(gameWorldHill.TailwindPoints)).Value);

        var gameResult =
            Domain._2.Game.Game.Create(gameId, globalGameSettings, gamePlayers, gameJumpers, competitionHill);
        if (gameResult.IsOk)
        {
            var game = gameResult.ResultValue;
            logger.Debug($"Started game: {game}");

            await games.Add(game, ct);
            await scheduler.ScheduleAsync(
                jobType: "StartPreDraft",
                payloadJson: json.Serialize(new { GameId = gameGuid }),
                runAt: clock.Now().AddSeconds(15),
                uniqueKey: $"StartPreDraft:{gameGuid}", ct: ct
            );
            await gameNotifier.GameStartedAfterMatchmaking(command.MatchmakingId, gameGuid);
            await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(game));
            return new Result(gameGuid);
        }

        throw new GameInitializationException(gameGuid, gameResult.ErrorValue.ToString());
    }
}

public class GameInitializationException(Guid gameId, string? message = null) : Exception(message);
using App.Application._2.Acl;
using App.Application._2.Commanding;
using App.Application._2.Messaging.Notifiers;
using App.Application._2.Messaging.Notifiers.Mapper;
using App.Application._2.Policy.GameHillSelector;
using App.Application._2.Policy.GameJumpersSelector;
using App.Application._2.Utility;
using App.Domain._2.Competition;
using App.Domain._2.Game;
using App.Domain._2.Matchmaking;
using Microsoft.FSharp.Collections;
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
    ICompetitionJumperAcl competitionJumperAcl, IMyLogger logger)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        logger.Debug($"Starting game for a matchmaking {command.MatchmakingId}");
        var matchmaking = await matchmakings.GetById(MatchmakingId.NewMatchmakingId(command.MatchmakingId), ct);
            
        if (!matchmaking.HasSucceeded)
        {
            throw new Exception("Matchmaking has not succeeded");
        }
        
        logger.Info($"Starting game for a succeeded matchmaking {command.MatchmakingId}");

        var selectedJumperDtos = await jumpersSelector.Select();

        var selectedHillDto = await hillSelector.Select();

        var gameGuid = guid.NewGuid();
        var gameId = Domain._2.Game.GameId.NewGameId(gameGuid);

        var endedMatchmakingPlayers = matchmaking.Players_;
        var gamePlayersEnumerable = endedMatchmakingPlayers.Select(matchmakingPlayer =>
        {
            var gamePlayerGuid = guid.NewGuid();
            var gamePlayerId = PlayerId.NewPlayerId(gamePlayerGuid);
            var matchmakingPlayerNickString = Domain._2.Matchmaking.PlayerModule.NickModule.value(matchmakingPlayer.Nick)!;
            var gamePlayerNick = PlayerModule.NickModule.createWithSuffix(matchmakingPlayerNickString).Value;
            var gamePlayer = new Domain._2.Game.Player(gamePlayerId, gamePlayerNick);
            return gamePlayer;
        });
        var gamePlayers = Domain._2.Game.PlayersModule.create(ListModule.OfSeq(gamePlayersEnumerable)).ResultValue;

        var gameJumpersEnumerable = selectedJumperDtos.Select(gameWorldJumperDto =>
        {
            var gameJumperId = guid.NewGuid();
            gameJumperAcl.Map(gameGuid, gameWorldJumperDto.Id, new GameJumperDto(gameJumperId));
            var competitionJumperId = guid.NewGuid(); // Od razu tworzymy competition jumpera, z którego korzystać będą inne Use Case'y
            competitionJumperAcl.Map(gameGuid, gameJumperId, new CompetitionJumperDto(competitionJumperId));
            return new Domain._2.Game.Jumper(Domain._2.Game.JumperId.NewJumperId(gameJumperId));
        });
        var gameJumpers = Domain._2.Game.JumpersModule.create(ListModule.OfSeq(gameJumpersEnumerable));

        var competitionHillId = Domain._2.Competition.HillId.NewHillId(selectedHillDto.Id);
        var competitionHill = new Domain._2.Competition.Hill(competitionHillId,
            HillModule.KPointModule.tryCreate(selectedHillDto.KPoint).Value,
            HillModule.HsPointModule.tryCreate(selectedHillDto.HsPoint).Value,
            HillModule.GatePointsModule.tryCreate(selectedHillDto.GatePoints).Value,
            HillModule.WindPointsModule.tryCreate(selectedHillDto.HeadwindPoints).Value,
            HillModule.WindPointsModule.tryCreate(selectedHillDto.TailwindPoints).Value);
        
        var gameResult = Domain._2.Game.Game.Create(gameId, globalGameSettings, gamePlayers, gameJumpers, competitionHill);
        if (gameResult.IsOk)
        {
            var game = gameResult.ResultValue;
            await games.Add(game, ct);
            await scheduler.ScheduleAsync(
                jobType: "StartPreDraft",
                payloadJson: json.Serialize(new { GameId = gameGuid }),
                runAt: clock.Now().AddSeconds(15),
                uniqueKey: $"StartPreDraft:{gameGuid}", ct:ct
                );
            await gameNotifier.GameStartedAfterMatchmaking(command.MatchmakingId, gameGuid);
            await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(game));   
        }

        return new Result(gameGuid);
    }
}
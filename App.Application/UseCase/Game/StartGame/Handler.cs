using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.JumpersForm;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Policy.GameHillSelector;
using App.Application.Policy.GameJumpersSelector;
using App.Application.Utility;
using App.Domain.Game;
using App.Domain.GameWorld;
using App.Domain.Matchmaking;
using Microsoft.FSharp.Collections;
using CompetitionJumperDto = App.Application.Acl.CompetitionJumperDto;
using HillModule = App.Domain.Competition.HillModule;
using PlayerId = App.Domain.Game.PlayerId;
using PlayerModule = App.Domain.Game.PlayerModule;

namespace App.Application.UseCase.Game.StartGame;

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
    Domain.Game.Settings globalGameSettings,
    IScheduler scheduler,
    IClock clock,
    IGameHillSelector hillSelector,
    IGuid guid,
    IGameJumperAcl gameJumperAcl,
    ICompetitionJumperAcl competitionJumperAcl,
    IMyLogger logger,
    IHills gameWorldHills,
    ICompetitionHillAcl competitionHillAcl,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IJumperGameFormAlgorithm jumperGameFormAlgorithm,
    IJumperGameFormStorage jumperGameFormStorage,
    IGameSchedule gameSchedule,
    IBotRegistry botRegistry)
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

        var selectedJumperDtos = (await jumpersSelector.Select(ct)).ToImmutableList();

        var selectedHillGuid = await hillSelector.Select(ct);
        var gameWorldHill = await gameWorldHills.GetById(Domain.GameWorld.HillId.NewHillId(selectedHillGuid), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(selectedHillGuid));

        var gameGuid = guid.NewGuid();
        var gameId = Domain.Game.GameId.NewGameId(gameGuid);

        Dictionary<Guid, Guid> gamePlayerByMatchmakingPlayer = new();

        var endedMatchmakingPlayers = matchmaking.Players_;
        var gamePlayersEnumerable = endedMatchmakingPlayers.Select(matchmakingPlayer =>
        {
            var gamePlayerGuid = guid.NewGuid();
            var gamePlayerId = PlayerId.NewPlayerId(gamePlayerGuid);
            var matchmakingPlayerNickString =
                Domain.Matchmaking.PlayerModule.NickModule.value(matchmakingPlayer.Nick)!;
            var gamePlayerNick = PlayerModule.NickModule.createWithSuffix(matchmakingPlayerNickString).Value;
            var gamePlayer = new Domain.Game.Player(gamePlayerId, gamePlayerNick);
            gamePlayerByMatchmakingPlayer.Add(matchmakingPlayer.Id.Item, gamePlayerGuid);
            botRegistry.RegisterGameBot(gameGuid, gamePlayerGuid);
            return gamePlayer;
        });
        var gamePlayers = Domain.Game.PlayersModule.create(ListModule.OfSeq(gamePlayersEnumerable)).ResultValue;

        var jumperGameFormsPrintString = "Forma zawodników:\n";
        var gameJumpersEnumerable = new List<Domain.Game.Jumper>();

        // Musimy użyć foreach, żeby zbudować debugowy jumperGameFormsPrintString
        foreach (var selectedGameWorldJumperDto in selectedJumperDtos)
        {
            var gameJumperId = guid.NewGuid();
            var gameJumperDto = new GameJumperDto(gameJumperId);
            var gameWorldJumperDto = new GameWorldJumperDto(selectedGameWorldJumperDto.Id);
            gameJumperAcl.Map(gameWorldJumperDto, gameJumperDto);

            var competitionJumperId = guid.NewGuid();
            competitionJumperAcl.Map(gameJumperDto, new CompetitionJumperDto(competitionJumperId));

            var liveForm = selectedGameWorldJumperDto.LiveForm;
            var gameForm = jumperGameFormAlgorithm.CalculateFromLiveForm(liveForm);
            jumperGameFormStorage.Add(gameJumperId, gameForm);

            jumperGameFormsPrintString += $"{selectedGameWorldJumperDto.Name} {selectedGameWorldJumperDto.Surname
            } --> GameForm {gameForm}\n";

            gameJumpersEnumerable.Add(new Domain.Game.Jumper(Domain.Game.JumperId.NewJumperId(gameJumperId)));
        }

        logger.Info(jumperGameFormsPrintString + "\n\n");
        var gameJumpers = Domain.Game.JumpersModule.create(ListModule.OfSeq(gameJumpersEnumerable));

        var competitionHillId = Domain.Competition.HillId.NewHillId(guid.NewGuid());
        var competitionHill = new Domain.Competition.Hill(competitionHillId,
            HillModule.KPointModule.tryCreate(Domain.GameWorld.HillModule.KPointModule.value(gameWorldHill.KPoint))
                .Value,
            HillModule.HsPointModule
                .tryCreate(Domain.GameWorld.HillModule.HsPointModule.value(gameWorldHill.HsPoint)).Value,
            HillModule.GatePointsModule
                .tryCreate(Domain.GameWorld.HillModule.GatePointsModule.value(gameWorldHill.GatePoints)).Value,
            HillModule.WindPointsModule
                .tryCreate(Domain.GameWorld.HillModule.WindPointsModule.value(gameWorldHill.HeadwindPoints)).Value,
            HillModule.WindPointsModule
                .tryCreate(Domain.GameWorld.HillModule.WindPointsModule.value(gameWorldHill.TailwindPoints)).Value);
        competitionHillAcl.Map(new CompetitionHillDto(competitionHillId.Item),
            new GameWorldHillDto(gameWorldHill.Id.Item));

        var gameResult =
            Domain.Game.Game.Create(gameId, globalGameSettings, gamePlayers, gameJumpers, competitionHill);
        if (gameResult.IsOk)
        {
            var game = gameResult.ResultValue;
            var timeToPreDraft = game.Settings.BreakSettings.BreakBeforePreDraft.Value;
            await games.Add(game, ct);
            gameSchedule.SchedulePhase(gameGuid, GamePhase.PreDraft, timeToPreDraft);
            await scheduler.ScheduleAsync(
                jobType: "StartPreDraft",
                payloadJson: json.Serialize(new { GameId = gameGuid }),
                runAt: clock.Now().Add(timeToPreDraft),
                uniqueKey: $"StartPreDraft:{gameGuid}", ct: ct
            );
            await gameNotifier.GameStartedAfterMatchmaking(command.MatchmakingId, gameGuid,
                gamePlayerByMatchmakingPlayer);
            await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(game, ct: ct));
            return new Result(gameGuid);
        }

        throw new GameInitializationException(gameGuid, gameResult.ErrorValue.ToString());
    }
}

public class GameInitializationException(Guid gameId, string? message = null) : Exception(message);
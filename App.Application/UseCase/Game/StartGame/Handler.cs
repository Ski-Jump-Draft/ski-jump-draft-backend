using System.Collections.Immutable;
using App.Application.Acl;
using App.Application.Bot;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game;
using App.Application.Game.Settings;
using App.Application.JumpersForm;
using App.Application.Matchmaking;
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
using GameJumperDto = App.Application.Acl.GameJumperDto;
using Hill = App.Domain.Competition.Hill;
using HillId = App.Domain.Competition.HillId;
using HillModule = App.Domain.Competition.HillModule;
using Player = App.Domain.Matchmaking.Player;
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
    IGameSettingsFactory gameSettingsFactory,
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
    IBotRegistry botRegistry,
    IRandom random,
    IPremiumMatchmakingGames premiumMatchmakingGames)
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

        var selectedGameWorldJumperDtos = await SelectAndShuffleGameJumpers(ct);
        var selectedHillGuid = await hillSelector.Select(ct);
        var gameWorldHill = await gameWorldHills.GetById(Domain.GameWorld.HillId.NewHillId(selectedHillGuid), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(selectedHillGuid));

        var gameGuid = guid.NewGuid();
        var gameId = Domain.Game.GameId.NewGameId(gameGuid);

        Dictionary<Guid, Guid> gamePlayerByMatchmakingPlayer = new();

        var gamePlayers = BuildGamePlayersFromMatchmaking(matchmaking, gameGuid, gamePlayerByMatchmakingPlayer);
        var gameJumpers = SetupGameJumpers(gameGuid, selectedGameWorldJumperDtos);
        var competitionHill = SetupCompetitionHill(gameWorldHill);

        var gameSettings = await gameSettingsFactory.Create(command.MatchmakingId);

        var gameResult =
            Domain.Game.Game.Create(gameId, gameSettings, gamePlayers, gameJumpers, competitionHill);

        if (!gameResult.IsOk) throw new GameInitializationException(gameGuid, gameResult.ErrorValue.ToString());
        var game = gameResult.ResultValue;
        var timeToPreDraft = game.Settings.BreakSettings.BreakBeforePreDraft.Value;
        await games.Add(game, ct);
        var belongsToPremiumMatchmaking =
            await premiumMatchmakingGames.StartGameIfBelongsToMatchmaking(command.MatchmakingId, gameGuid);
        logger.Info($"Game {gameGuid} belongs to premium matchmaking: {belongsToPremiumMatchmaking}");
        await SchedulePreDraftPhase(gameGuid, timeToPreDraft, ct);
        await NotifyGameStart(game, gameGuid, gamePlayerByMatchmakingPlayer, command.MatchmakingId, ct);
        return new Result(gameGuid);
    }

    private async Task<ImmutableList<SelectedGameWorldJumperDto>> SelectAndShuffleGameJumpers(CancellationToken ct)
    {
        return (await jumpersSelector.Select(ct)).ToList().Shuffle(random).ToImmutableList();
    }

    private Hill SetupCompetitionHill(Domain.GameWorld.Hill gameWorldHill)
    {
        var competitionHillId = Domain.Competition.HillId.NewHillId(guid.NewGuid());
        var competitionHill = CreateCompetitionHill(competitionHillId, gameWorldHill);
        SetupCompetitionHillAcl(competitionHillId, gameWorldHill);
        return competitionHill;
    }

    private Jumpers SetupGameJumpers(Guid gameId, ImmutableList<SelectedGameWorldJumperDto> selectedJumperDtos)
    {
        var jumperGameFormsPrintString = "Forma zawodników:\n";
        var gameJumpersEnumerable = new List<Domain.Game.Jumper>();

        foreach (var selectedGameWorldJumperDto in selectedJumperDtos)
        {
            var gameJumperId = guid.NewGuid();
            var gameJumperDto = new GameJumperDto(gameId, gameJumperId);
            SetupJumperAcl(selectedGameWorldJumperDto, gameJumperDto);
            jumperGameFormsPrintString =
                SetUpAndLogJumperForm(gameJumperId, selectedGameWorldJumperDto, jumperGameFormsPrintString);
            gameJumpersEnumerable.Add(new Domain.Game.Jumper(Domain.Game.JumperId.NewJumperId(gameJumperId)));
        }

        logger.Info(jumperGameFormsPrintString + "\n\n");
        var gameJumpers = Domain.Game.JumpersModule.create(ListModule.OfSeq(gameJumpersEnumerable));
        return gameJumpers;
    }

    private Players BuildGamePlayersFromMatchmaking(Domain.Matchmaking.Matchmaking matchmaking, Guid gameGuid,
        Dictionary<Guid, Guid> gamePlayerByMatchmakingPlayer)
    {
        var matchmakingId = matchmaking.Id_.Item;
        var endedMatchmakingPlayers = matchmaking.Players_;
        var gamePlayersList = CreateGamePlayersFromMatchmakingPlayers(matchmakingId, gameGuid, endedMatchmakingPlayers,
            gamePlayerByMatchmakingPlayer);
        var shuffledGamePlayers = ShuffleGamePlayers(gamePlayersList);
        // var sortedGamePlayers = OrderGamePlayersByNick(gamePlayersList);
        var gamePlayers = Domain.Game.PlayersModule.create(ListModule.OfSeq(shuffledGamePlayers)).ResultValue;
        return gamePlayers;
    }

    private static IOrderedEnumerable<Domain.Game.Player> OrderGamePlayersByNick(
        List<Domain.Game.Player> gamePlayersList)
    {
        return gamePlayersList.OrderBy(player => PlayerModule.NickModule.value(player.Nick));
    }

    private IEnumerable<Domain.Game.Player> ShuffleGamePlayers(List<Domain.Game.Player> gamePlayersList)
    {
        return gamePlayersList.Shuffle(random);
    }

    private void SetupCompetitionHillAcl(HillId competitionHillId, Domain.GameWorld.Hill gameWorldHill)
    {
        competitionHillAcl.Map(new CompetitionHillDto(competitionHillId.Item),
            new GameWorldHillDto(gameWorldHill.Id.Item));
    }

    private Hill CreateCompetitionHill(HillId competitionHillId, Domain.GameWorld.Hill gameWorldHill)
    {
        return new Domain.Competition.Hill(competitionHillId,
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
    }

    private string? SetUpAndLogJumperForm(Guid gameJumperId,
        SelectedGameWorldJumperDto selectedGameWorldJumperDto,
        string? jumperGameFormsPrintString)
    {
        var liveForm = selectedGameWorldJumperDto.LiveForm;
        var gameForm = jumperGameFormAlgorithm.CalculateFromLiveForm(liveForm);
        jumperGameFormStorage.Add(gameJumperId, gameForm);

        if (jumperGameFormsPrintString is null) return null;

        jumperGameFormsPrintString += $"{selectedGameWorldJumperDto.Name} {selectedGameWorldJumperDto.Surname
        } --> GameForm {gameForm}\n";
        return jumperGameFormsPrintString;
    }

    private void SetupJumperAcl(SelectedGameWorldJumperDto selectedGameWorldJumperDto, GameJumperDto gameJumperDto)
    {
        var gameWorldJumperDto = new GameWorldJumperDto(selectedGameWorldJumperDto.GameWorldJumperId);
        logger.Info($"[ACL] Mapping GameWorldJumper -> GameJumper | Game:{gameJumperDto.GameId} GWJ:{
            gameWorldJumperDto.GameWorldJumperId} -> GJ:{gameJumperDto.GameJumperId}");

        gameJumperAcl.Map(gameWorldJumperDto, gameJumperDto);

        var competitionJumperId = guid.NewGuid();
        logger.Info($"[ACL] Mapping GameJumper -> CompetitionJumper | GJ:{gameJumperDto.GameJumperId} -> CJ:{
            competitionJumperId}");
        competitionJumperAcl.Map(gameJumperDto, new CompetitionJumperDto(competitionJumperId));
    }


    private List<Domain.Game.Player> CreateGamePlayersFromMatchmakingPlayers(Guid matchmakingId, Guid gameGuid,
        IReadOnlyCollection<Player> endedMatchmakingPlayers,
        Dictionary<Guid, Guid> gamePlayerByMatchmakingPlayer)
    {
        return endedMatchmakingPlayers.Select(matchmakingPlayer =>
        {
            var gamePlayerGuid = guid.NewGuid();
            var gamePlayerId = PlayerId.NewPlayerId(gamePlayerGuid);
            var matchmakingPlayerNickString =
                Domain.Matchmaking.PlayerModule.NickModule.value(matchmakingPlayer.Nick)!;
            var gamePlayerNick = PlayerModule.NickModule.createWithSuffix(matchmakingPlayerNickString).Value;
            var gamePlayer = new Domain.Game.Player(gamePlayerId, gamePlayerNick);
            gamePlayerByMatchmakingPlayer.Add(matchmakingPlayer.Id.Item, gamePlayerGuid);
            RegisterGameBotIfNeeded(matchmakingId, matchmakingPlayer, gameGuid, gamePlayerGuid);
            return gamePlayer;
        }).ToList();
    }

    private bool RegisterGameBotIfNeeded(Guid matchmakingId, Player matchmakingPlayer, Guid gameGuid,
        Guid gamePlayerGuid)
    {
        if (!botRegistry.IsMatchmakingBot(matchmakingId, matchmakingPlayer.Id.Item)) return false;
        botRegistry.RegisterGameBot(gameGuid, gamePlayerGuid);
        return true;
    }

    private async Task SchedulePreDraftPhase(Guid gameGuid, TimeSpan timeToPreDraft, CancellationToken ct)
    {
        gameSchedule.ScheduleEvent(gameGuid, GameScheduleTarget.PreDraft, timeToPreDraft);
        await scheduler.ScheduleAsync(
            jobType: "StartPreDraft",
            payloadJson: json.Serialize(new { GameId = gameGuid }),
            runAt: clock.Now().Add(timeToPreDraft),
            uniqueKey: $"StartPreDraft:{gameGuid}", ct: ct
        );
    }

    private async Task NotifyGameStart(Domain.Game.Game game, Guid gameGuid,
        Dictionary<Guid, Guid> gamePlayerByMatchmakingPlayer, Guid matchmakingId,
        CancellationToken ct)
    {
        Command command;
        await gameNotifier.GameStartedAfterMatchmaking(matchmakingId, gameGuid,
            gamePlayerByMatchmakingPlayer);
        await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(game, ct: ct));
    }
}

public class GameInitializationException(Guid gameId, string? message = null) : Exception(message);
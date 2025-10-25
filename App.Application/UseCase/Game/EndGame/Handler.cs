using System.Text.Json;
using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.DraftTurnIndexes;
using App.Application.Game.PassPicksCount;
using App.Application.Game.Ranking;
using App.Application.Matchmaking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
using App.Application.Telemetry;
using App.Application.Utility;
using App.Domain.Game;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace App.Application.UseCase.Game.EndGame;

public record Command(
    Guid GameId
) : ICommand<Result>;

public record Result();

public class Handler(
    IGames games,
    IGameNotifier gameNotifier,
    IMyLogger logger,
    IGameRankingFactorySelector gameRankingFactorySelector,
    GameUpdatedDtoMapper gameUpdatedDtoMapper,
    IPremiumMatchmakingGames premiumMatchmakingGames,
    ITelemetry telemetry,
    IClock clock,
    IDraftTurnIndexesArchive draftTurnIndexesArchive,
    IDraftPassPicksCountArchive draftPassPicksCountArchive)
    : ICommandHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command command, CancellationToken ct)
    {
        var game = await games.GetById(GameId.NewGameId(command.GameId), ct)
            .AwaitOrWrap(_ => new IdNotFoundException(command.GameId));

        logger.Info($"Ending game {command.GameId}");

        var gameRankingFactory = gameRankingFactorySelector.Select(game.Settings.RankingPolicy);
        var gameRanking = await gameRankingFactory.Create(game, ct);

        var gamePlayers = PlayersModule.toList(game.Players);
        var nickByPlayerIdDictionary = gamePlayers.ToDictionary(player => player.Id, player => player.Nick);
        var nickByPlayerIdFSharpMap =
            MapModule.OfSeq(nickByPlayerIdDictionary.Select(kvp =>
                new Tuple<PlayerId, string>(kvp.Key, PlayerModule.NickModule.value(kvp.Value))));
        logger.Info($"Ended game (ID: {command.GameId}) ranking:\n{gameRanking.PrettyPrint(nickByPlayerIdFSharpMap)}");

        var endedGameResult = game.EndGame(gameRanking);

        await premiumMatchmakingGames.EndGameIfRuns(command.GameId);

        if (!endedGameResult.IsOk) return new Result();

        var endedGame = endedGameResult.ResultValue;
        await games.Add(endedGame, ct);
        await gameNotifier.GameUpdated(await gameUpdatedDtoMapper.FromDomain(endedGame, ct: ct));

        var telemetryGameRanking = await CreateEndedGamePlayersForTelemetry(command.GameId, gameRanking);

        await telemetry.Record(new GameTelemetryEvent("GameEnded", command.GameId,
            null, null, clock.Now(),
            new Dictionary<string, object>()
            {
                ["Ranking"] = JsonSerializer.Serialize(telemetryGameRanking),
                ["DraftOrderPolicy"] = game.Settings.DraftSettings.Order.ToString(),
                ["RankingPolicy"] = game.Settings.RankingPolicy.ToString(),
                ["PlayersCount"] = game.PlayersCount
            }));

        return new Result();
    }

    private async Task<List<TelemetryEndedGamePlayerDto>> CreateEndedGamePlayersForTelemetry(Guid gameId,
        Domain.Game.Ranking ranking)
    {
        var passPicksCountByPlayer = await draftPassPicksCountArchive.GetDictionary(gameId);
        var turnIndexesDtos = await draftTurnIndexesArchive.GetAsync(gameId);
        var dtoByPlayerId = turnIndexesDtos.ToDictionary(dto => dto.gamePlayerId, dto => dto);

        var dtosList = ranking.PositionsAndPoints.Select(keyValuePair =>
        {
            var playerId = keyValuePair.Key;
            var (position, points) = keyValuePair.Value;

            var turnIndexesDto = dtoByPlayerId[playerId.Item];
            passPicksCountByPlayer.TryGetValue(playerId.Item, out var passPicksCount);

            return new TelemetryEndedGamePlayerDto(playerId.Item, RankingModule.PointsModule.value(points),
                RankingModule.PositionModule.value(position), turnIndexesDto.FixedTurnIndex,
                turnIndexesDto.TurnIndexes, passPicksCount);
        }).ToList();

        return dtosList;
    }

    private record TelemetryEndedGamePlayerDto(
        Guid GamePlayerId,
        int Points,
        int Position,
        int? DraftFixedTurnIndex, // If the draft order policy is not random
        List<int>? DraftRandomTurnIndexes, // If the draft order policy is random
        int PassPicksCount
    );
}
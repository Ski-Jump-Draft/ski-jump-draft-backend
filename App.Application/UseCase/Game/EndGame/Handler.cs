using App.Application.Commanding;
using App.Application.Exceptions;
using App.Application.Extensions;
using App.Application.Game.Ranking;
using App.Application.Messaging.Notifiers;
using App.Application.Messaging.Notifiers.Mapper;
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
    IGameRankingFactorySelector gameRankingFactorySelector)
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

        if (!endedGameResult.IsOk) return new Result();

        var endedGame = endedGameResult.ResultValue;
        await games.Add(endedGame, ct);
        await gameNotifier.GameUpdated(GameUpdatedDtoMapper.FromDomain(endedGame));

        return new Result();
    }
}
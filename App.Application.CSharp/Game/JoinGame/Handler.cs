using App.Application.CSharp.Exception;
using App.Application.CSharp.Ext;
using App.Application.CSharp.Game.Exception;
using App.Domain;
using App.Domain.Game;
using Microsoft.FSharp.Control;
using App.Domain.Shared;
using App.Domain.Repositories;

using GameErrors = App.Domain.Game.GameModule.Error; // taki fajny myk

namespace App.Application.CSharp.Game.JoinGame;

public record Command(
    App.Domain.Draft.
    Ids.PlayerId PlayerId,
    Ids.GameId GameId
);

public class Handler(IGameRepository games, IPlayerRepository players)
{
    public async Task HandleAsync(Command command, CancellationToken ct) {
        var playerId = command.PlayerId;
        var player = await FSharpAsyncExt.AwaitOrThrow(players.GetById(command.PlayerId), ct,
            new IdNotFoundException(playerId.Item));
        var gameId = command.GameId;
        var game = await FSharpAsyncExt.AwaitOrThrow(games.GetById(gameId), ct, new IdNotFoundException(gameId.Item));

        var joinResult = game.Join(playerId);
        if (joinResult.IsOk)
        {
            var gameAfterJoin = joinResult.ResultValue;
            await FSharpAsyncExt.AwaitOrThrow(
                games.Update(command.GameId, gameAfterJoin), ct,
                new JoiningGameFailedUnknownException(player, gameAfterJoin)
            );
        }
        else
        {
            var error = joinResult.ErrorValue;
            
            switch (error)
            {
                case GameErrors.PlayerAlreadyJoined:
                    throw new GameFullException(game);
                case GameErrors.EndingMatchmakingTooFewPlayers:
                    throw new PlayerAlreadyJoinedException(game, player);
                case GameErrors.InvalidPhase(var excepted, var actual):
                    throw new JoiningGameInvalidPhaseException(game, actual);
                default:
                    throw new JoiningGameFailedUnknownException(player, game);
            }
        }
    }
}
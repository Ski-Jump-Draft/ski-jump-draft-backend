using App.Application.CSharp.Exception;
using App.Application.CSharp.Ext;
using App.Application.CSharp.Game.Exception;
using Microsoft.FSharp.Control;
using App.Domain.Shared;
using App.Domain.Repositories;

namespace App.Application.CSharp.Game.JoinGame;

public record Command(
    Ids.PlayerId PlayerId,
    Ids.GameId GameId
);

public class Handler(IGameRepository games, IPlayerRepository players)
{
    public async Task HandleAsync(Command command, CancellationToken ct)
    {
        var playerId = command.PlayerId;
        var player = await FSharpAsyncExt.AwaitOrThrow(players.GetById(command.PlayerId), ct,
            new IdNotFoundException(playerId.Item));
        var gameId = command.GameId;
        var game = await FSharpAsyncExt.AwaitOrThrow(games.GetById(gameId), ct, new IdNotFoundException(gameId.Item));

        if (!game.CanJoin(player))
        {
            throw new GameFullException(game);
        }

        await FSharpAsyncExt.AwaitOrThrow(games.Join(gameId, playerId), ct,
            new JoiningGameFailedException(player, game));
    }
}